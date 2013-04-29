using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;
using EveComFramework.Core;
using EveComFramework.Move;

namespace MinerBot
{
    class DroneDefense : State
    {
        public Entity CurTarget;
        public bool InCombat = false;

        public DroneDefense()
        {
            DefaultFrequency = 5000;
        }

        public bool Combat(object[] Params)
        {
            if (Entity.All.Count(ent => ent.IsTargetingMe) == 0)
            {
                Drone.AllInSpace.Where(drone => drone.State != EntityState.Departing && drone.State != EntityState.Departing_2).ReturnToDroneBay();
                if (Drone.AllInSpace.Count() > 0)
                {
                    return false;
                }
                InCombat = false;
                return true;
            }
            if (CurTarget == null || CurTarget.Exploded)
            {
                CurTarget = Entity.All.First(ent => ent.IsTargetingMe);
                return false;
            }
            if (CurTarget.Distance < MyShip.MaxTargetRange && !CurTarget.LockedTarget && !CurTarget.LockingTarget)
            {
                CurTarget.LockTarget();
                return false;
            }
            if (!CurTarget.IsActiveTarget && CurTarget.LockedTarget)
            {
                CurTarget.MakeActive();
                return false;
            }
            if (CurTarget.Distance < 20000 && CurTarget.IsActiveTarget && Drone.AllInSpace.Count(drone => drone.GroupID == Group.CombatDrone && drone.Target != CurTarget) > 0)
            {
                List<Drone> drones = Drone.AllInSpace.Where(drone => drone.GroupID == Group.CombatDrone && drone.Target != CurTarget).ToList();
                EVEFrame.Log("Attacking with " + drones.Count);
                drones.Attack();
                return false;
            }
            if (Drone.AllInBay.Count(drone => drone.GroupID == Group.CombatDrone) > 0 && Me.MaxActiveDrones > Drone.AllInSpace.Count())
            {
                Drone.AllInBay.Where(drone => drone.GroupID == Group.CombatDrone).Take(Me.MaxActiveDrones - Drone.AllInSpace.Count()).Launch();
                return false;
            }
            return false;
        }

        public bool WaitForTarget(object[] Params)
        {
            if (Entity.All.Count(ent => ent.IsTargetingMe) > 0)
            {
                QueueState(Combat);
                QueueState(WaitForTarget);
                InCombat = true;
                return true;
            }
            return false;
        }

        public void Activate()
        {
            if (Idle)
            {
                QueueState(WaitForTarget);
            }
        }

        public void Deactivate()
        {
            if (!InCombat)
            {
                Clear();
            }
        }
    }
}
