using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;
using EveComFramework.Core;
using EveComFramework.Move;
using EveComFramework.Cargo;
using InnerSpaceAPI;

namespace MinerBot
{
    #region Enums

    public enum DropoffType
    {
        ItemHangar,
        CorpHangar,
        Jetcan
    }

    #endregion

    #region Settings

    class MinerSettings : Settings
    {
        public DropoffType DropoffType = DropoffType.ItemHangar;
        public String Dropoff = "";
    }

    #endregion

    class Bot : State
    {
        #region Instantiation

        static Bot _Instance;
        public static Bot Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Bot();
                }
                return _Instance;
            }
        }

        private Bot() : base()
        {

        }

        #endregion        

        #region Variables

        public Queue<Entity> Belts = new Queue<Entity>();
        public Entity CurBelt = null;
        public Entity CurRoid = null;
        public Entity CurTarget = null;
        public double CurRoidOre = 0;
        public double EstimatedMined = 0;
        public bool Active = false;
        Dictionary<Module, int> Cycles = null;
        Dictionary<Module, double> CyclePos = null;
        Dictionary<Module, Entity> CycleTarget = null;
        Dictionary<long, long> UsedRoidList = new Dictionary<long, long>();

        bool Rescan = false;

        public DroneDefense drones = new DroneDefense();
        public JetcanDeploy jetcans = new JetcanDeploy();

        public Logger Console = new Logger();
        public MinerSettings Config = new MinerSettings();
        public Move Move = Move.Instance;
        public Cargo Cargo = Cargo.Instance;

        #endregion

        #region Actions

        public bool TemporaryIsPrimedCheck(InventoryContainer Cont)
        {
            try
            {
                double test = Cont.UsedCapacity;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public int UsedRoidListUpdate(string[] args)
        {
            try
            {
                Console.Log("Adding roid " + args[2] + " to used list for miner " + args[1]);
                UsedRoidList.AddOrUpdate(long.Parse(args[1]), long.Parse(args[2]));
            }
            catch { }

            return 0;
        }

        public void RegisterCommands()
        {
            LavishScriptAPI.LavishScript.Commands.AddCommand("MiningTeamUpdateRoidList", UsedRoidListUpdate);
        }

        #endregion

        #region States

        public bool Traveling(object[] Params)
        {
            if (!Move.Idle || !Cargo.Idle || (Session.InSpace && MyShip.ToEntity.Mode == EntityMode.Warping))
            {
                return false;
            }
            return true;
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
                Console.Log("Opening Inventory");
                Command.OpenInventory.Execute();
                return false;
            }
            if (!TemporaryIsPrimedCheck(MyShip.OreHold))
            {
                Console.Log("Making OreHold Active");
                MyShip.OreHold.MakeActive();
                return false;
            }
            if (MyShip.OreHold.Items.Count(item => item.CategoryID == Category.Asteroid) > 0)
            {
                Console.Log("Transfering Ore");
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
                if (!TemporaryIsPrimedCheck(Station.ItemHangar))
                {
                    Station.ItemHangar.MakeActive();
                    return false;
                }
                if (Station.ItemHangar.Items.Where(a => !a.isUnpacked).GroupBy(a => a.Type).Any(a => a.Count() > 1))
                {
                    Station.ItemHangar.StackAll();
                    return false;
                }

                Console.Log("Undocking");
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
                CycleTarget = new Dictionary<Module, Entity>();
                foreach (Module Laser in MyShip.Modules.Where(MiningLasers))
                {
                    Cycles.Add(Laser, 0);
                    CyclePos.Add(Laser, 0);
                    CycleTarget.Add(Laser, null);
                }
            }

            if (Entity.All.Count(ent => ent.CategoryID == Category.Asteroid && !UsedRoidList.ContainsValue(ent.ID)) == 0)
            {
                CurBelt = null;
                QueueState(PrepareToWarp);
                QueueState(HeadingToBelt);
                return true;
            }

            if (Config.DropoffType == DropoffType.Jetcan && jetcans.Idle)
            {
                jetcans.QueueState(jetcans.JetCan);
            }
            if (Config.DropoffType != DropoffType.Jetcan && !jetcans.Idle)
            {
                jetcans.Clear();
            }

            if (drones.Idle)
            {
                drones.Activate();
            }

            if (MyShip.OreHold == null)
            {
                Console.Log("Opening Inventory");
                Command.OpenInventory.Execute();
                return false;
            }
            if (!TemporaryIsPrimedCheck(MyShip.OreHold))
            {
                Console.Log("Making OreHold Active");
                MyShip.OreHold.MakeActive();
                return false;
            }
            if (MyShip.OreHold.UsedCapacity > MyShip.OreHold.MaxCapacity * 0.95)
            {
                Console.Log("Preparing to Unload");
                QueueState(PrepareUnload);
                return true;
            }

            if (MyShip.OreHold.Items.Where(a => !a.isUnpacked).GroupBy(a => a.Type).Any(a => a.Count() > 1))
            {
                MyShip.OreHold.StackAll();
                return false;
            }

            if (CurRoid == null || !CurRoid.Exists)
            {
                CurRoid = Entity.All.Where(ent => ent.CategoryID == Category.Asteroid && !UsedRoidList.ContainsValue(ent.ID)).OrderBy(ent => ent.Distance).First();
                foreach (Module Laser in Cycles.Keys.ToList())
                {
                    Cycles[Laser] = 0;
                    CyclePos[Laser] = 0;
                }
                if (CurRoid != null)
                {
                    LavishScriptAPI.LavishScript.ExecuteCommand("relay \"all other\" -noredirect MiningTeamUpdateRoidList " + Me.CharID + " " + CurRoid.ID.ToString());
                }

                Rescan = false;
            }

            if (CurRoid.Distance > MyShip.Modules.Where(MiningLasers).Min(mod => mod.OptimalRange))
            {
                if (MyShip.ToEntity.Mode == EntityMode.Approaching)
                {
                    return false;
                }
                Console.Log("Approaching " + CurRoid.Name);
                CurRoid.Approach((int)(MyShip.Modules.Where(MiningLasers).Min(mod => mod.OptimalRange) * 0.75));
                return false;
            }

            if (!CurRoid.LockedTarget && !CurRoid.LockingTarget)
            {
                Console.Log("Locking " + CurRoid.Name);
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
                if (Laser.IsActive)
                {
                    if (CycleTarget[Laser] == null || !CycleTarget[Laser].ActiveModules.Contains(Laser))
                    {
                        Cycles[Laser] = 0;
                        CycleTarget[Laser] = Entity.Targets.FirstOrDefault(ent => ent.ActiveModules.Contains(Laser));
                    }
                }
            }

            EstimatedMined = (double)MyShip.Modules.Where(MiningLasers).Sum(mod => mod.MiningYield * (mod.Completion + Cycles[mod]));

            if (MyShip.Modules.Count(mod => mod.GroupID == Group.SurveyScanner) > 0)
            {
                if ((!SurveyScan.Scan.ContainsKey(CurRoid) || Rescan) && !MyShip.Modules.First(mod => mod.GroupID == Group.SurveyScanner).IsActive)
                {
                    Console.Log("Activating Scanner");
                    MyShip.Modules.First(mod => mod.GroupID == Group.SurveyScanner).Activate();
                    Rescan = false;
                    return false;
                }
                if (SurveyScan.Scan.ContainsKey(CurRoid))
                {
                    CurRoidOre = SurveyScan.Scan[CurRoid] * CurRoid.Volume;

                    if (EstimatedMined > (SurveyScan.Scan[CurRoid] * CurRoid.Volume + 5))
                    {
                        Console.Log("ShortCycling Lasers");
                        Rescan = true;
                        foreach (Module laser in MyShip.Modules.Where(MiningLasers))
                        {
                            laser.Deactivate();
                            Cycles[laser] = 0;
                        }
                    }
                }
            }

            if ((double)MyShip.Modules.Where(MiningLasers).Sum(mod => mod.MiningYield * mod.Completion) + MyShip.OreHold.UsedCapacity > MyShip.OreHold.MaxCapacity)
            {
                foreach (Module laser in MyShip.Modules.Where(MiningLasers))
                {
                    laser.Deactivate();
                    Cycles[laser] = 0;
                }
            }

            if (MyShip.Modules.Where(MiningLasers).Count(mod => !mod.IsActive) > 0 && CurRoid.LockedTarget)
            {
                Console.Log("Mining " + CurRoid.Name);
                MyShip.Modules.Where(MiningLasers).First(mod => !mod.IsActive).Activate(CurRoid);
                return false;
            }

            foreach (Module laser in MyShip.Modules.Where(MiningLasers).Where(mod => mod.IsActive && !CurRoid.ActiveModules.Contains(mod)))
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
            switch (Config.DropoffType)
            {
                case DropoffType.ItemHangar:
                    QueueState(PrepareToWarp);
                    QueueState(DockAtStation);
                    break;
                case DropoffType.Jetcan:
                    QueueState(InBelt);
                    break;
            }
            return true;
        }

        public bool PrepareToWarp(object[] Params)
        {
            if (drones.InCombat && !drones.Idle)
            {
                drones.Deactivate();
            }
            if (!jetcans.Idle)
            {
                jetcans.Clear();
            }
            if (!drones.Idle)
            {
                Console.Log("drones is not idle.");
            }
            return drones.Idle;
        }

        public bool HeadingToBelt(object[] Params)
        {
            if (Session.InStation)
            {
                QueueState(InStation);
                return true;
            }
            if (CurBelt == null || !CurBelt.Exists)
            {
                if (Belts.Count == 0)
                {
                    Belts = new Queue<Entity>(Entity.All.Where(ent => ent.GroupID == Group.AsteroidBelt && ent.Type != "Ice Field"));
                    if (Belts.Count == 0)
                    {
                        Console.Log("Error, no belts found");
                        return true;
                    }
                }
                CurBelt = Belts.Dequeue();
                return false;
            }
            if (MyShip.ToEntity.Mode != EntityMode.Warping && CurBelt.Distance > 100000)
            {
                Console.Log("Warping To " + CurBelt.Name + " | " + CurBelt.Type);
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
                    Console.Log("Docking At " + station.Name);
                    station.Dock();
                    NextPulse = DateTime.Now.AddSeconds(10);
                    return false;
                }
            }
            return false;
        }

        #endregion

    }

    #region Utility classes

    static class DictionaryHelper
    {
        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }

    #endregion

}


