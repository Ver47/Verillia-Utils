using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;


namespace Celeste.Mod.Verillia.Utils
{
    public class SpeedBonus : Component
    {
        protected Actor actor => EntityAs<Actor>();

        internal SpeedBonus()
            : base(true, false)
        {
        }

        public override void Update()
        {
            base.Update();
            var overpass = actor.GetOverpass();
            overpass.Active = false;
            var next = Move(overpass.H, overpass.V);
            overpass.Active = true;
            overpass.H = (int)Math.Round(next.X);
            overpass.V = (int)Math.Round(next.Y);
        }

        public virtual Vector2 GetLiftSpeed(Vector2 orig)
        {
            return Vector2.Zero;
        }

        public virtual Vector2 Move(int overH, int overV) { return new Vector2(overH, overV); }
    }
}
