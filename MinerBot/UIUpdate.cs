using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EveCom;
using EveComFramework.Core;

namespace MinerBot
{
    class UIUpdate : State
    {
        #region Instantiation

        static UIUpdate _Instance;
        public static UIUpdate Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new UIUpdate();
                }
                return _Instance;
            }
        }

        private UIUpdate() : base()
        {
            QueueState(Update);
        }

        #endregion        

        #region Variables

        public List<String> Bookmarks = new List<String>();

        #endregion

        #region States

        public bool Update(object[] Params)
        {
            Bookmarks = Bookmark.All.Select(a => a.Title).ToList();

            return false;
        }

        #endregion
    }
}
