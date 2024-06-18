using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Monocle;

namespace Celeste.Mod.Verillia.Utils
{
    public static class CWMethodExts
    {
        public static void Add(this Entity self, ComponentWrapper component)
        {
            self.Add(component.component);
        }
    }
    public abstract class ComponentWrapper
    {
        private Component _component;
        protected internal Component component
        {
            get
            {
                return _component;
            }
            set
            {
                if (!componentType.IsInstanceOfType(value))
                {
                    throw new ArgumentException("Component is of incorrect type.");
                }
                _component = value;
            }
        }
        public Type componentType { get; private set; }
        protected ComponentWrapper(Component component, Type componentType)
        {
            this.componentType = componentType;
            this.component = component;
        }
    }
}
