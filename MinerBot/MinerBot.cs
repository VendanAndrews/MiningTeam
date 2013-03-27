using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EveCom;

namespace MinerBot
{
    public partial class MinerBot : Form
    {
        public MinerBot()
        {
            InitializeComponent();
        }

        enum State
        {
            InStation,
            Unloaded,
            HeadingToBelt,
            InBelt,
            HeadingToStation,
            Defensive,
        }

        State CurState;
        DateTime NextPulse;
        Queue<Entity> Belts = new Queue<Entity>();
        Entity CurBelt = null;
        Entity CurRoid = null;
        Entity CurTarget = null;
        double CurRoidOre = 0;
        double EstimatedMined = 0;
        bool Active = false;
        Dictionary<Module, int> Cycles = null;
        Dictionary<Module, double> CyclePos = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            using (new EVEFrameLock())
            {
            }
            CurState = State.InStation;
            NextPulse = DateTime.Now.AddSeconds(1);
            EVEFrame.OnFrame += new EventHandler(EVEFrame_OnFrame);
            txtStation.Text = Properties.Settings.Default.Station;
        }

        void EVEFrame_OnFrame(object sender, EventArgs e)
        {
            if (!Active)
            {
                return;
            }
            if (Session.Safe && Session.NextSessionChange < Session.Now)
            {
                //lblState.Text = "State: " + CurState.ToString();
                if (DateTime.Now > NextPulse)
                {
                    NextPulse = DateTime.Now.AddSeconds(2);
                    switch (CurState)
                    {
                        case State.InStation:
                            if (Session.InSpace)
                            {
                                CurState = State.InBelt;
                                return;
                            }
                            if (MyShip.OreHold == null)
                            {
                                EVEFrame.Log("Opening Inventory");
                                Command.OpenInventory.Execute();
                                return;
                            }
                            if (!MyShip.OreHold.IsPrimed)
                            {
                                EVEFrame.Log("Making OreHold Active");
                                MyShip.OreHold.MakeActive();
                            }
                            if (MyShip.OreHold.Items.Count(item => item.CategoryID == Category.Asteroid) > 0)
                            {
                                EVEFrame.Log("Transfering Ore");
                                MyShip.OreHold.Items.Where(item => item.CategoryID == Category.Asteroid).MoveTo(Station.ItemHangar);
                            }
                            CurState = State.Unloaded;
                            break;
                        case State.Unloaded:
                            if (Session.InStation)
                            {
                                EVEFrame.Log("Undocking");
                                Command.CmdExitStation.Execute();
                                NextPulse = DateTime.Now.AddSeconds(10);
                                return;
                            }
                            CurState = State.InBelt;
                            break;
                        case State.InBelt:
                            Func<Module, bool> MiningLasers = (mod) => mod.GroupID == Group.FrequencyMiningLaser || mod.GroupID == Group.MiningLaser || mod.GroupID == Group.StripMiner;
                            if (Cycles == null)
                            {
                                Cycles = new Dictionary<Module, int>();
                                CyclePos = new Dictionary<Module, double>();
                                foreach (Module Laser in MyShip.Modules.Where(MiningLasers))
                                {
                                    Cycles.Add(Laser, 0);
                                    CyclePos.Add(Laser, 0);
                                }
                            }


                            if (Entity.All.Count(ent => ent.IsTargetingMe) > 0)
                            {
                                CurState = State.Defensive;
                                return;
                            }

                            if (Entity.All.Count(ent => ent.CategoryID == Category.Asteroid) == 0)
                            {
                                CurBelt = null;
                                CurState = State.HeadingToBelt;
                                return;
                            }
                            if (MyShip.OreHold == null)
                            {
                                EVEFrame.Log("Opening Inventory");
                                Command.OpenInventory.Execute();
                                return;
                            }
                            if (!MyShip.OreHold.IsPrimed)
                            {
                                EVEFrame.Log("Making OreHold Active");
                                MyShip.OreHold.MakeActive();
                            }
                            if (MyShip.OreHold.UsedCapacity > MyShip.OreHold.MaxCapacity * 0.75)
                            {
                                EVEFrame.Log("Returning To Station");
                                CurState = State.HeadingToStation;
                                return;
                            }
                            if (CurRoid == null || !CurRoid.Exists)
                            {
                                CurRoid = Entity.All.Where(ent => ent.CategoryID == Category.Asteroid).OrderBy(ent => ent.Distance).First();
                                foreach (Module Laser in Cycles.Keys.ToList())
                                {
                                    Cycles[Laser] = 0;
                                    CyclePos[Laser] = 0;
                                }
                            }
                            
                            if (CurRoid.Distance > MyShip.Modules.Where(MiningLasers).Min(mod => mod.OptimalRange))
                            {
                                if(MyShip.ToEntity.Mode == EntityMode.Approaching)
                                {
                                    return; 
                                }
                                EVEFrame.Log("Approaching " + CurRoid.Name);
                                CurRoid.Approach((int)(MyShip.Modules.Where(MiningLasers).Min(mod => mod.OptimalRange) * 0.75));
                                return;
                            }

                            if (!CurRoid.LockedTarget && !CurRoid.LockingTarget)
                            {
                                EVEFrame.Log("Locking " + CurRoid.Name);
                                CurRoid.LockTarget();
                                return;
                            }

                            foreach (Module Laser in MyShip.Modules.Where(MiningLasers))
                            {
                                if (Laser.Completion < CyclePos[Laser])
                                {
                                    Cycles[Laser]++;
                                }
                                CyclePos[Laser] = Laser.Completion;
                            }

                            EstimatedMined = (double)MyShip.Modules.Where(MiningLasers).Sum(mod => mod.MiningYield * (mod.Completion+Cycles[mod]));



                            if(MyShip.Modules.Count(mod => mod.GroupID == Group.SurveyScanner) > 0)
                            {
                                if (!SurveyScan.Scan.ContainsKey(CurRoid) && !MyShip.Modules.First(mod => mod.GroupID == Group.SurveyScanner).IsActive)
                                {
                                    EVEFrame.Log("Activating Scanner");
                                    MyShip.Modules.First(mod => mod.GroupID == Group.SurveyScanner).Activate();
                                    return;
                                }
                                if (SurveyScan.Scan.ContainsKey(CurRoid))
                                {
                                    CurRoidOre = SurveyScan.Scan[CurRoid] * CurRoid.Volume;

                                    if (EstimatedMined > (SurveyScan.Scan[CurRoid] * CurRoid.Volume + 5))
                                    {
                                        EVEFrame.Log("ShortCycling Lasers");
                                        foreach (Module laser in MyShip.Modules.Where(MiningLasers))
                                        {
                                            laser.Deactivate();
                                        }
                                    }
                                }
                            }

                            if (MyShip.Modules.Where(MiningLasers).Count(mod => !mod.IsActive) > 0 && CurRoid.LockedTarget)
                            {
                                EVEFrame.Log("Mining " + CurRoid.Name);
                                MyShip.Modules.Where(MiningLasers).First(mod => !mod.IsActive).Activate(CurRoid);
                            }
                            break;
                        case State.HeadingToBelt:
                            if (CurBelt == null || !CurBelt.Exists)
                            {
                                if (Belts.Count == 0)
                                {
                                    Belts = new Queue<Entity>(Entity.All.Where(ent => ent.GroupID == Group.AsteroidBelt));
                                }
                                CurBelt = Belts.Dequeue();
                                return;
                            }
                            if (MyShip.ToEntity.Mode != EntityMode.Warping && CurBelt.Distance > 100000)
                            {
                                EVEFrame.Log("Warping To " + CurBelt.Name);
                                CurBelt.WarpTo();
                                NextPulse = DateTime.Now.AddSeconds(10);
                                return;
                            }
                            if (CurBelt.Distance < 100000)
                            {
                                CurState = State.InBelt;
                            }
                            break;
                        case State.HeadingToStation:
                            if (Session.InStation)
                            {
                                CurState = State.InStation;
                                return;
                            }
                            if (MyShip.ToEntity.Mode != EntityMode.Warping)
                            {
                                Entity station = Entity.All.FirstOrDefault(ent => ent.GroupID == Group.Station && ent.Name == Properties.Settings.Default.Station);
                                if (station != null && station.Exists)
                                {
                                    EVEFrame.Log("Docking At " + CurRoid.Name);
                                    station.Dock();
                                    NextPulse = DateTime.Now.AddSeconds(10);
                                    return;
                                }
                            }
                            break;
                        case State.Defensive:
                            if (Entity.All.Count(ent => ent.IsTargetingMe) == 0)
                            {
                                Drone.AllInSpace.Where(drone => drone.State != EntityState.Departing && drone.State != EntityState.Departing_2).ReturnToDroneBay();
                                if (Drone.AllInSpace.Count() > 0)
                                {
                                    return;
                                }
                                CurState = State.InBelt;
                                return;
                            }
                            if (CurTarget == null || CurTarget.Exploded)
                            {
                                CurTarget = Entity.All.First(ent => ent.IsTargetingMe);
                                return;
                            }
                            if (CurTarget.Distance < MyShip.MaxTargetRange)
                            {
                                CurTarget.LockTarget();
                            }
                            if (!CurTarget.IsActiveTarget)
                            {
                                CurTarget.MakeActive();
                            }
                            if (CurTarget.Distance < 20000 && CurTarget.IsActiveTarget && Drone.AllInSpace.Count(drone => drone.GroupID == Group.CombatDrone && drone.Target != CurTarget) > 0)
                            {
                                Drone.AllInSpace.Where(drone => drone.GroupID == Group.CombatDrone && drone.Target != CurTarget).Attack();
                                NextPulse = DateTime.Now.AddSeconds(5);
                                return;
                            }
                            if (Drone.AllInBay.Count(drone => drone.GroupID == Group.CombatDrone) > 0 && Me.MaxActiveDrones > Drone.AllInSpace.Count())
                            {
                                Drone.AllInBay.Where(drone => drone.GroupID == Group.CombatDrone).Take(Me.MaxActiveDrones - Drone.AllInSpace.Count()).Launch();
                                NextPulse = DateTime.Now.AddSeconds(5);
                                return;
                            }
                            break;
                    }
                }
            }
        }

        private void txtStation_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Station = txtStation.Text;
            Properties.Settings.Default.Save();
        }

        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            Active = chkActive.Checked;
        }

        private void btnCurrentStation_Click(object sender, EventArgs e)
        {
            using(new EVEFrameLock())
            {
                txtStation.Text = Session.StationName;
                Properties.Settings.Default.Station = txtStation.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblState.Text = "State: " + CurState.ToString();
            lblCurRoidOre.Text = "CurRoid Ore: " + CurRoidOre.ToString("F2") + " m^3";
            lblEstimatedMined.Text = "EstimatedMined: " + EstimatedMined.ToString("F2") + " m^3";
        }
    }
}