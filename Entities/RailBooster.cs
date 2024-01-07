using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod;
using Celeste;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Verillia.Utils.Entities {
    [Tracked]
    [CustomEntity("Verillia/Utils/RailBooster/Node")]
    public class RailBooster
    {
        public bool Entry = true;
        public List<RailRope> Rails;
        public void AddRail(RailRope rail)
        {
            int amount =  Rails.Count();
            if (amount > 1)
            {
                Rails.Add(rail);
                return;
            }
            for(int index = 1; index < amount; index++)
            {

            }
        }
    }
}
