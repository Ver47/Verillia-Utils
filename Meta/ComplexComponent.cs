using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Pico8;
using Monocle;

namespace Celeste.Mod.Verillia.Utils
{
    public class ComplexComponent : Component
    {
        private static double i = 0;

        public ComplexComponent(bool active, bool visible)
            : base(active, visible) { }

        public virtual void PreUpdate() { }
        public virtual void PostUpdate() { }

        public virtual void PreRender() { }
        public virtual void PostRender() { }
    }
}
