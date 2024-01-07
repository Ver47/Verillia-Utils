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
    public class RailBooster : Entity
    {
        public bool Entry = true;
        public List<RailRope> Rails;
        public int? PrioritizedIndex { get; private set; } = 0;
        public int HighestPriority { get; private set; } = 0;

        public RailBooster() { }

        public override void Render()
        {
            base.Render();
        }

        public override void Update()
        {
            base.Update();
        }

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

        public int CycleY(int index, bool down)
        {
            return 0;
        }

        public int CycleX(int index, bool right)
        {
            return 0;
        }

        public float getTimeLimit()
        {
            return 0.25f+(Rails.Count()*0.5f);
        }

        public int getDefault(int index)
        {
            switch (Rails.Count())
            {
                default:
                    if (PrioritizedIndex is int PIndex)
                        return PIndex;
                    return index;
                case 1:
                    return 0;
                case 2:
                    if (index == -1)
                        return -1;
                    return (index == 0) ? 1 : 0;
            }
        }

        public bool isExit()
        {
            return Rails.Count() <= 1;
        }
    }
}
