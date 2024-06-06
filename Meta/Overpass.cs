using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Verillia.Utils
{
    public class Overpass : Component
    {
        public int H { get; internal set; }
        public int V { get; internal set; }

        internal Overpass() : base (true, false)
        {
            H = V = 0;
        }

        public override void Update()
        {
            base.Update();
        }

        internal void Reset()
        {
            H = V = 0;
        }
    }
}
