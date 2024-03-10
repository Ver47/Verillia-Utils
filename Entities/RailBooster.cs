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
    [CustomEntity("VerUtils/RailBooster-Node")]
    public class RailBooster : Entity
    {
        //InstantChoice determines if the player immediately chooses on entry.
        public readonly bool InstantChoice = false;
        public bool IsEntry { get; private set; }
        public float ReentryTimer = 0f;
        public List<RailRope> Rails = new();
        public int? PrioritizedIndex { get; private set; } = 0;
        public int HighestPriority { get; private set; } = 0;

        public Sprite sprite;
        public VertexLight light;

        public const float CenterSuckSpeed = 180f;
        public const float CenterSuckTime = 0.05f;

        public const float BaseTime = 0.125f;
        public const float RailTime = 0.125f;
        public const float ReentryTime = 2.0f;

        public RailBooster(EntityData data, Vector2 offset) :
            this(offset+data.Position,
                data.Bool("entry", true),
                data.Bool("instant", false)) { }

        public RailBooster(Vector2 position, bool isEntry, bool instantChoice) : base(position)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                "Constructing");
            IsEntry = isEntry;
            InstantChoice = instantChoice;
            Position = position;
            Depth = IsEntry?
                VerUtils.Depths.RailBooster_Entry : VerUtils.Depths.RailBooster_Node;
            Collider = new Circle(10f, 0f, 2f);
            Add(sprite = GFX.SpriteBank.Create(isEntry ? "boosterRed" : "booster"));
            Add(new PlayerCollider(OnPlayer));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
        }

        public void OnPlayer(Player player)
        {
            if (player.StateMachine.State ==
                player.GetVerUtilsExt().StRailBoost
                || ReentryTimer > 0
                || !IsEntry)
            {
                ReentryTimer = ReentryTime;
                return;
            }
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Node",
                "Railboosted");
            player.StateMachine.State = 
                player.GetVerUtilsExt().StRailBoost;
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Update()
        {
            base.Update();
            ReentryTimer -= Engine.DeltaTime;
        }

        public void AddRail(RailRope rail)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                "Adding Rail");
            int amount =  Rails.Count;
            if (amount < 1)
            {
                Rails.Add(rail);
                return;
            }
            for(int index = 1; index < amount; index++)
            {
                // Insertion logic goes here.
            }
        }

        public float getTimeLimit()
        {
            return InstantChoice?
                0 : BaseTime + ((Rails.Count - 1) * RailTime);
        }

        public int CycleY(int index, bool down)
        {
            // Return new index from inputted direction
            return index;
        }

        public int CycleX(int index, bool right)
        {
            // Return new index from inputted direction
            return index;
        }

        public int getClosestToDirection(Vector2 direction)
        {
            return 0;
        }

        public int getDefault(int index)
        {
            switch (Rails.Count)
            {
                case 1:
                    return 0;
                case 2:
                    if (index == -1)
                        return -1;
                    return (index == 0) ? 1 : 0;
                default:
                    if (PrioritizedIndex is int PIndex)
                        return PIndex;
                    return index;
            }
        }

        public bool Exit(bool JustEntered)
        {
            if (Rails.Count <= (JustEntered? 0 : 1)){
                ReentryTimer = ReentryTime;
                return true;
            }
            return false;
        }
    }
}
