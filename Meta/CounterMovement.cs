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
    public sealed class CounterMovement : ComplexComponent
    {
        public int H { get; private set; }
        public int V { get; private set; }

        public int GoalH { get; internal set; }
        public int GoalV { get; internal set; }

        internal CounterMovement() : base (true, false)
        {
            PostUpdate();
        }

        public override void PostUpdate()
        {
            GoalH = GoalV = H = V = 0;
        }

        internal void RemoveH(int h)
        {
            GoalH -= h;
            var Sign = Math.Sign(H);
            H -= h;
            if (Math.Sign(H) != Sign)
            {
                H = 0;
            }
        }

        internal void RemoveV(int v)
        {
            GoalV -= v;
            var Sign = Math.Sign(V);
            V -= v;
            if (Math.Sign(V) != Sign)
            {
                V = 0;
            }
        }
    }
}
