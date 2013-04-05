using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;

namespace MinerBot
{
    class Move : State
    {
        public bool Busy = false;

        public Move()
        {
            DefaultFrequency = 1000;
        }

        public void DockAt(Func<IDockable> Dockable)
        {
            QueueState(DockAtState, -1, Dockable);
            Busy = true;
        }

        public void DockAt(IDockable Dockable)
        {
            DockAt(() => Dockable);
        }

        bool DockAtState(object[] Params)
        {
            if (Session.InStation)
            {
                return true;
            }
            Params = Params ?? new object[] { };
            IDockable Target;
            if (Params.Length == 0)
            {
                return true;
            }
            Target = ((Func<IDockable>)Params[0])();
            Target.Dock();
            WaitFor(10, () => Session.InStation, () => MyShip.ToEntity.Mode == EntityMode.Warping);
            QueueState(DockAtState, -1, Params);
            return true;
        }
    }
}
