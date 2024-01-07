using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;

namespace Celeste.Mod.Verillia.Utils.Entities
{

    [CustomEntity(
        "Verillia/Utils/NumFlag/Float=Float",
        "Verillia/Utils/NumFlag/Int=Int"
        )]
    public class Variable : Entity
    {
        public Variable():base() { }

        public static Variable Float(EntityData data){
            VerilliaUtilsModule.Session.Floats[data.Attr("flag")] = data.Float("value");
            return new Variable();
        }

        public static Variable Int(EntityData data)
        {
            VerilliaUtilsModule.Session.Ints[data.Attr("flag")] = data.Int("value");
            return new Variable();
        }
    }
}