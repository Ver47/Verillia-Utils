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
    public class VerilliaUtilsPlayerExt : Component
    {
        public Vector2[] RailBoosterPath;
        public static Dictionary<string, int> MovementModes;

        public VerilliaUtilsPlayerExt(bool active = true, bool visible = true) : base(active, visible)
        {
        }

        public override void Update()
        {

        }
    }
}
