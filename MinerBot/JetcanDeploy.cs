using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;

namespace MinerBot
{
    class JetcanDeploy : State
    {
        public DateTime LastDrop = DateTime.Now.AddMinutes(-3);
        public List<Entity> Jetcans = new List<Entity>();
        public Entity CurJetcan = null;

        public JetcanDeploy()
            : base()
        {
            DefaultFrequency = 5000;
        }

        public bool JetCan(object[] Params)
        {
            if (CurJetcan != null)
            {
                if (CurJetcan.CanCargo == null)
                {
                    CurJetcan.OpenCargo();
                    return false;
                }
                if (!CurJetcan.CanCargo.IsPrimed)
                {
                    CurJetcan.CanCargo.MakeActive();
                    return false;
                }
                if (CurJetcan.CanCargo.UsedCapacity > CurJetcan.CanCargo.MaxCapacity * 0.95)
                {
                    Jetcans = Jetcans.Where(ent => ent.Exists && !ent.Exploded && ent.Distance < 2500).ToList();
                    CurJetcan = Jetcans.FirstOrDefault(can => can.CanCargo != null && can.CanCargo.IsPrimed && can.CanCargo.UsedCapacity < CurJetcan.CanCargo.MaxCapacity * 0.95);
                    return false;
                }
                if (MyShip.OreHold.UsedCapacity > MyShip.OreHold.MaxCapacity * 0.1)
                {
                    MyShip.OreHold.Items.Where(item => item.CategoryID == Category.Asteroid).MoveTo(CurJetcan.CanCargo);
                    return false;
                }
            }
            if (CurJetcan == null && MyShip.OreHold.UsedCapacity > MyShip.OreHold.MaxCapacity * 0.5 && (DateTime.Now - LastDrop).TotalMinutes > 3)
            {
                MyShip.OreHold.Items.Where(item => item.CategoryID == Category.Asteroid).Jettison();
                LastDrop = DateTime.Now;
                WaitFor(4, () => Entity.All.Count(ent => ent.GroupID == Group.CargoContainer && ent.Distance < 2500 && !Jetcans.Contains(ent)) > 0);
                QueueState(RenameCan);
                QueueState(JetCan);
                return true;
            }

            return false;
        }

        public bool RenameCan(object[] Params)
        {
            Entity NewCan = Entity.All.FirstOrDefault(ent => ent.GroupID == Group.CargoContainer && ent.Distance < 2500 && !Jetcans.Contains(ent));
            if (NewCan != null)
            {
                NewCan.OpenCargo();
                Jetcans.Add(NewCan);
                CurJetcan = NewCan;
            }
            return true;
        }




    }
}
