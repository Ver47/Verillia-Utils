using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Verillia.Utils
{
    [Tracked]
    public class VerUtilsEventListener : Component
    {
        public string Name;
        public Action OnEvent;
        public VerUtilsEventListener(bool active, string EventName, Action onEvent):
            base(active, true){
            Name = EventName;
            OnEvent = onEvent;
        }
        public void Run()
        {
            if (Active)
                OnEvent();
        }
    }
}
