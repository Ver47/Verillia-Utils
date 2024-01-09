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
using Celeste.Mod.Verillia.Utils.Meta;

namespace Celeste.Mod.Verillia.Utils.Entities {
    [Tracked]
    [CustomEntity("Verillia/Utils/RailBooster/Node")]
    public class RailBooster : Entity
    {
        public bool Entry;
        public List<RailRope> Rails;
        public int? PrioritizedIndex { get; private set; } = 0;
        public int HighestPriority { get; private set; } = 0;

        public Sprite sprite;
        public VertexLight light;

        public static readonly Vector2 PlayerOffset = new Vector2(0f, -2f);
        public const float CenterSuckSpeed = 80f;
        public const float BaseTime = 0.125f;
        public const float RailTime = 0.125f;

        public RailBooster(Vector2 position, bool isEntry = false)
        {
            Entry = isEntry;
            Position = position;
            Depth = VerilliaUtilsDepths.RailBoosterNode;
            Collider = new Circle(10f, 0f, 2f);
            Add(sprite = GFX.SpriteBank.Create(isEntry ? "boosterRed" : "booster"));
            Add(new PlayerCollider(OnPlayer));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
        }

        public void OnPlayer(Player player)
        {
            player.StateMachine.ForceState(player.Components.Get<VerilliaUtilsPlayerExt>().RailBoostState);
        }

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
            int amount =  Rails.Count;
            if (amount > 1)
            {
                Rails.Add(rail);
                return;
            }
            for(int index = 1; index < amount; index++)
            {
                // Insertion logic goes here.
            }
        }

        public int CycleY(int index, bool down)
        {
            // Return new index from inputted direction
            return 0;
        }

        public int CycleX(int index, bool right)
        {
            // Return new index from inputted direction
            return 0;
        }

        public float getTimeLimit()
        {
            return BaseTime + ((Rails.Count - 1) * RailTime);
        }

        public int getDefault(int index)
        {
            switch (Rails.Count)
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
            return Rails.Count <= 1;
        }
    }
}
