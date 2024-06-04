using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;


namespace Celeste.Mod.Verillia.Utils
{
    public class LiftSpeedBonus : Component
    {
        protected Actor actor => EntityAs<Actor>();

        internal LiftSpeedBonus()
            : base(true, false)
        {
        }

        public virtual Vector2 GetSpeed()
        {
            return Vector2.Zero;
        }
    }
}
