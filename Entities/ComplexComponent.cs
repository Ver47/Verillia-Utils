using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Pico8;
using Monocle;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    public class ComplexComponent : Component
    {
        public ComplexComponent(bool active, bool visible)
            : base(active, visible) { }

        public virtual void PreUpdate() { }
        public virtual void PostUpdate() { }

        public virtual void PreRender() { }
        public virtual void PostRender() { }
    }
}
