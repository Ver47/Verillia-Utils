using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Verillia.Utils.Triggers
{
    [CustomEntity("Verillia/Utils/FlagSettlingStateTrigger")]
    public class FlagSettlingStateTrigger : Trigger
    {
        public enum Types
        {
            Bool,
            Int,
            String
        }
        public Types Type;
        public bool toActive;
        public string flag;

        public FlagSettlingStateTrigger(EntityData data, Vector2 position) : base(data, position)
        {
            flag = data.Attr("flag");
            toActive = data.Bool("active", true);
            Type = data.Enum<Types>("type");
        }
        public void SetBoolFlagSettleState(string flag, bool active)
        {
            if (active){
                VerilliaUtilsModule.Session.SettledFlags.Add(flag, (Scene as Level).Session.Flags.Contains(flag));
                return;
            }
            VerilliaUtilsModule.Session.SettledFlags.Remove(flag);
        }
        public void SetIntFlagSettleState(string flag, bool active)
        {
            if (active)
            {
                VerilliaUtilsModule.Session.SettledInts.Add(flag, VerilliaUtilsModule.Session.Ints["flag"]);
            }
            VerilliaUtilsModule.Session.SettledFlags.Remove(flag);
        }
    }
}
