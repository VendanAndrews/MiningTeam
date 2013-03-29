using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;

namespace MinerBot
{
    class Bot : State
    {
        public Queue<Entity> Belts = new Queue<Entity>();
        public Entity CurBelt = null;
        public Entity CurRoid = null;
        public Entity CurTarget = null;
        public double CurRoidOre = 0;
        public double EstimatedMined = 0;
        public bool Active = false;
        Dictionary<Module, int> Cycles = null;
        Dictionary<Module, double> CyclePos = null;

        public DroneDefense drones = new DroneDefense();

        public DropoffType Dropoff = DropoffType.ItemHangar;

        public enum DropoffType
        {
            ItemHangar,
            CorpHangar,
            Jetcan
        }

        public bool InStation(object[] Params)
        {
            if (Session.InSpace)
            {
                QueueState(InBelt);
                return true;
            }
            if (MyShip.OreHold == null)
            {
                EVEFrame.Log("Opening Inventory");
                Command.OpenInventory.Execute();
                return false;
            }
            if (!MyShip.OreHold.IsPrimed)
            {
                EVEFrame.Log("Making OreHold Active");
                MyShip.OreHold.MakeActive();
                return false;
            }
            if (MyShip.OreHold.Items.Count(item => item.CategoryID == Category.Asteroid) > 0)
            {
                EVEFrame.Log("Transfering Ore");
                MyShip.OreHold.Items.Where(item => item.CategoryID == Category.Asteroid).MoveTo(Station.ItemHangar);
                return false;
            }
            QueueState(Unloaded);
            return true;
        }

        public bool Unloaded(object[] Params)
        {
            if (Session.InStation)
            {
                EVEFrame.Log("Undocking");
                Command.CmdExitStation.Execute();
                WaitFor(30, () => Session.InSpace);
                QueueState(Unloaded);
                return true;
            }
            QueueState(InBelt);
            return true;
        }

        public bool InBelt(object[] Params)
        {
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

            if (Entity.All.Count(ent => ent.CategoryID == Category.Asteroid) == 0)
            {
                CurBelt = null;
                QueueState(PrepareToWarp);
                QueueState(HeadingToBelt);
                return true;
            }

            if (!drones.Active)
            {
                drones.Activate();
            }

            if (MyShip.OreHold == null)
            {
                EVEFrame.Log("Opening Inventory");
                Command.OpenInventory.Execute();
                return false;
            }
            if (!MyShip.OreHold.IsPrimed)
            {
                EVEFrame.Log("Making OreHold Active");
                MyShip.OreHold.MakeActive();
                return false;
            }
            if (MyShip.OreHold.UsedCapacity > MyShip.OreHold.MaxCapacity * 0.75)
            {
                EVEFrame.Log("Preparing to Unload");
                QueueState(PrepareUnload);
                return true;
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
                if (MyShip.ToEntity.Mode == EntityMode.Approaching)
                {
                    return false;
                }
                EVEFrame.Log("Approaching " + CurRoid.Name);
                CurRoid.Approach((int)(MyShip.Modules.Where(MiningLasers).Min(mod => mod.OptimalRange) * 0.75));
                return false;
            }

            if (!CurRoid.LockedTarget && !CurRoid.LockingTarget)
            {
                EVEFrame.Log("Locking " + CurRoid.Name);
                CurRoid.LockTarget();
                return false;
            }

            foreach (Module Laser in MyShip.Modules.Where(MiningLasers))
            {
                if (Laser.Completion < CyclePos[Laser])
                {
                    Cycles[Laser]++;
                }
                CyclePos[Laser] = Laser.Completion;
            }

            EstimatedMined = (double)MyShip.Modules.Where(MiningLasers).Sum(mod => mod.MiningYield * (mod.Completion + Cycles[mod]));

            if (MyShip.Modules.Count(mod => mod.GroupID == Group.SurveyScanner) > 0)
            {
                if (!SurveyScan.Scan.ContainsKey(CurRoid) && !MyShip.Modules.First(mod => mod.GroupID == Group.SurveyScanner).IsActive)
                {
                    EVEFrame.Log("Activating Scanner");
                    MyShip.Modules.First(mod => mod.GroupID == Group.SurveyScanner).Activate();
                    return false;
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
                return false;
            }

            foreach(Module laser in MyShip.Modules.Where(MiningLasers).Where(mod => mod.IsActive && !CurRoid.ActiveModules.Contains(mod)))
            {
                laser.Deactivate();
                CyclePos[laser] = 0;
                Cycles[laser] = 0;
                return false;
            }
            return false;
        }

        public bool PrepareUnload(object[] Params)
        {
            switch (Dropoff)
            {
                case DropoffType.ItemHangar:
                    QueueState(PrepareToWarp);
                    QueueState(DockAtStation);
                    QueueState(InStation);

                    break;
            }
            return true;
        }

        public bool PrepareToWarp(object[] Params)
        {
            if (drones.Active)
            {
                drones.Deactivate();
            }
            return !drones.Busy;
        }

        public bool HeadingToBelt(object[] Params)
        {
            if (CurBelt == null || !CurBelt.Exists)
            {
                if (Belts.Count == 0)
                {
                    Belts = new Queue<Entity>(Entity.All.Where(ent => ent.GroupID == Group.AsteroidBelt));
                }
                CurBelt = Belts.Dequeue();
                return false;
            }
            if (MyShip.ToEntity.Mode != EntityMode.Warping && CurBelt.Distance > 100000)
            {
                EVEFrame.Log("Warping To " + CurBelt.Name);
                CurBelt.WarpTo();
                WaitFor(10, () => false, () => MyShip.ToEntity.Mode == EntityMode.Warping);
                QueueState(HeadingToBelt);
                return true;
            }
            if (CurBelt.Distance < 100000)
            {
                QueueState(InBelt);
                return true;
            }
            return false;
        }

        public bool DockAtStation(object[] Params)
        {
            if (Session.InStation)
            {
                QueueState(InStation);
                return true;
            }
            if (MyShip.ToEntity.Mode != EntityMode.Warping)
            {
                Entity station = Entity.All.FirstOrDefault(ent => ent.GroupID == Group.Station && ent.Name == Properties.Settings.Default.Station);
                if (station != null && station.Exists)
                {
                    EVEFrame.Log("Docking At " + CurRoid.Name);
                    station.Dock();
                    NextPulse = DateTime.Now.AddSeconds(10);
                    return false;
                }
            }
            return false;
        }
    }
}
