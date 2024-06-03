using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Celeste.LavaRect;
using static Celeste.Trigger;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    [Tracked]
    [CustomEntity("VerUtils/RailBooster-Node")]
    public class RailBooster : Entity
    {
        internal class PlayerRailBooster : Entity
        {
            internal Sprite sprite;
            private SoundSource sound;

            private Player player;
            private const float AttractSpeed = 20f;
            private Vector2 subpixel = Vector2.Zero;
            private Vector2 exactPosition => subpixel + Position;

            private static ParticleType particles;
            private static Color ParticleColor1 = Calc.HexToColor("a986d3");
            private static Color ParticleColor2 = Calc.HexToColor("563b85");
            private const float FalldownAccel = 16f;
            private const float PLifeMin = 1f;
            private const float PLifeMax = 1.5f;
            public enum Phases
            {
                Attract,
                Idle,
                Move,
                Burst
            }
            private bool Moving = false;
            private bool Bursted = false;
            public Phases Phase;


            public PlayerRailBooster(Vector2 position, Player pp)
            {
                Add(sprite = GFX.SpriteBank.Create("VerUtils-railbooster"));
                sprite.OnFinish = OnFinish;

                player = pp;
                Depth = VerUtils.Depths.RailBooster_Node - 1;
                Tag = Tag.WithTags(Tags.TransitionUpdate, Tags.Persistent);
                Phase = Phases.Attract;

                Position = position;

                Add(sound = new SoundSource());
                InitParticles();

                Add(new MirrorReflection());
            }

            private static void InitParticles()
            {
                if (particles is not null)
                    return;
                particles = new ParticleType(Booster.P_BurstRed);
                particles.Color = ParticleColor1;
                particles.Color2 = ParticleColor2;
                particles.Acceleration = Vector2.UnitY * FalldownAccel;
                particles.LifeMax = PLifeMax;
                particles.LifeMin = PLifeMin;
                particles.FadeMode = ParticleType.FadeModes.Late;
                particles.ColorMode = ParticleType.ColorModes.Choose;
            }

            public override void Update()
            {
                Vector2 goal = player.ExactPosition + player.Collider.Center;
                switch (Phase)
                {
                    case Phases.Attract:
                        Moving = true;
                        AnimPlayNoReset("loop");
                        MoveTo(Calc.Approach(exactPosition, goal, AttractSpeed * Engine.DeltaTime));
                        if (exactPosition == goal)
                            Phase = Phases.Idle;
                        break;
                    case Phases.Move:
                        if (!Moving)
                        {
                            Audio.Play("event:/game/05_mirror_temple/redbooster_dash", Position);
                            sound.Play("event:/game/05_mirror_temple/redbooster_move");
                            sound.DisposeOnTransition = false;
                        }
                        Moving = true;
                        AnimPlayNoReset("spin");
                        var dir = player.GetVerUtilsExt().Velocity.SafeNormalize();
                        var angle = (-dir).Angle();
                        // Idle but with particles falling down.if (Scene.OnInterval(0.02f))
                        if (Scene.OnInterval(0.02f))
                        {
                            (Scene as Level).ParticlesBG.Emit(
                                particles,
                                2,
                                player.Center - player.GetVerUtilsExt().Velocity.SafeNormalize() * 3f + new Vector2(0f, 3f),
                                new Vector2(3f, 3f),
                                angle
                                );
                        }
                        MoveTo(goal);
                        break;
                    case Phases.Idle:
                        MoveTo(goal);
                        break;
                    case Phases.Burst:
                        Burst();
                        break;
                }
                base.Update();
            }

            public void Idle()
            {
                if (Moving)
                    Audio.Play("event:/game/05_mirror_temple/redbooster_enter", Position);
                Moving = false;
                AnimPlayNoReset("inside");
                sound.Stop();
                Phase = Phases.Idle;
            }

            public void Burst()
            {
                if (!Bursted)
                    Audio.Play("event:/game/05_mirror_temple/redbooster_end", Position);
                sound.Stop();
                Bursted = true;
                AnimPlayNoReset("pop");
                Phase = Phases.Burst;
            }

            public void OnFinish(string name)
            {
                if (name == "pop")
                    RemoveSelf();
            }
            public void MoveTo(Vector2 pos)
            {
                int X = (int)Math.Round(pos.X);
                int Y = (int)Math.Round(pos.Y);
                Position = new Vector2(X, Y);
                subpixel = new Vector2(pos.X - X, pos.Y - Y);
            }
            public void AnimPlayNoReset(string name)
            {
                if (sprite.CurrentAnimationID != name)
                {
                    sprite.Play(name);
                }
            }
            public void SoundPlayNoReset(string name)
            {
                if (sound.EventName != name || sound.InstancePlaying)
                    sound.Play(name);
            }
        }

        //InstantChoice determines if the player immediately chooses on entry.
        public readonly bool InstantChoice = false;
        public bool IsEntry { get; private set; }
        public float ReentryTimer = 0f;
        public List<RailRope> Rails = new();
        public int? PrioritizedIndex { get; private set; }
        public int HighestPriority { get; private set; } = 0;

        private Sprite sprite;
        private VertexLight light;
        public Wiggler spawn;

        public const float CenterSuckSpeed = 180f;
        public const float CenterSuckTime = 0.2f;

        public const float BaseTime = 0.2f;
        public const float RailTime = 0.1f;
        public const float ReentryTime = 3.5f;

        public RailBooster(EntityData data, Vector2 offset) :
            this(offset + data.Position,
                data.Bool("isEntry", true),
                data.Bool("instant", false))
        { }

        public RailBooster(Vector2 position, bool isEntry, bool instantChoice) : base(position)
        {
            IsEntry = isEntry;
            InstantChoice = instantChoice;
            Position = position;
            Depth = IsEntry ?
                VerUtils.Depths.RailBooster_Entry : VerUtils.Depths.RailBooster_Node;
            Collider = new Circle(10f, 0f, 2f);

            Add(sprite = GFX.SpriteBank.Create("VerUtils-railbooster"));
            sprite.Play(IsEntry ? "loop" : "small");

            Add(new PlayerCollider(OnPlayer));
            Add(light = new VertexLight(Color.White, 1f, 16, 32));
            var trans = new TransitionListener();
            trans.OnInBegin = OnOut;
            Add(trans);

            Add(new MirrorReflection());

            Add(spawn = Wiggler.Create(0.5f, 4f, [MethodImpl(MethodImplOptions.NoInlining)] (float f) =>
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }));
        }

        public void Connect(Scene scene)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                "Attaching self to ropes.");
            foreach (RailRope rope in scene.Tracker.GetEntities<RailRope>())
            {
                if (rope.Rope.Begin == Position && rope.endA is null)
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                        "Found one! Adding...");
                    rope.endA = this;
                    AddRail(rope);
                    continue;
                }
                if (rope.Rope.End == Position && rope.endB is null)
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                        "Found one! Adding...");
                    rope.endB = this;
                    AddRail(rope);
                }
            }
        }

        private void OnOut()
        {
            foreach (var e in Scene.Tracker.GetEntities<RailBooster>())
            {
                var node = e as RailBooster;
                if (node.Position == Position
                    && node != this)
                {
                    //this is to help in transition smoothening
                    node.sprite.RemoveSelf();
                    var bubble = sprite;
                    sprite.RemoveSelf();
                    node.Add(node.sprite = bubble);
                    RemoveSelf();
                }
            }
        }

        public void OnPlayer(Player player)
        {
            var playerExt = player.GetVerUtilsExt();
            if (player.StateMachine.State == playerExt.StRailBoost
                || !IsEntry || ReentryTimer > 0)
                return;
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                "Railboosted");
            playerExt.LastRailBooster = this;
            var prb = new PlayerRailBooster(Position, player);
            prb.sprite.SetAnimationFrame(sprite.CurrentAnimationFrame);
            Scene.Add(playerExt.playerRailBooster = prb);
            sprite.Play("shrink");
            player.StateMachine.State =
                playerExt.StRailBoost;
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Update()
        {
            base.Update();
            bool WasNotActive = ReentryTimer > 0;
            ReentryTimer -= Engine.DeltaTime;
            if (IsEntry && WasNotActive && ReentryTimer <= 0)
            {
                spawn.Start();
                sprite.Play("grow");
            }
        }

        public void ResetTimer()
        {
            ReentryTimer = ReentryTime;
            sprite.Play("small");
        }

        public void AddRail(RailRope rail)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                "Adding Rail");
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                $"Previous amount of rails: {Rails.Count}");
            if (!Rails.Contains(rail))
                Rails.Add(rail);
            int amount = Rails.Count;
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                $"Current amount of rails: {amount}");
        }

        public float getTimeLimit()
        {
            return InstantChoice ?
                0 : BaseTime + (Math.Min((Rails.Count - 1), 12) * RailTime);
        }

        public int getClosestToDirection(Vector2 direction, int avoid)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                $"Comparing directions to: {direction}");
            Vector2 dir = direction;
            dir.Normalize();
            SimpleCurve curve = Rails[0].getPathFrom(Position);
            Vector2 best = (curve.Control - curve.Begin).SafeNormalize();
            int bestindex = 0;
            for (int index = 1; index < Rails.Count; index++)
            {
                if (index == avoid)
                    continue;
                RailRope rail = Rails[index];
                curve = rail.getPathFrom(Position);
                // The unit y * 8 thing is to help manage the issue of... uhh... weird inputs.
                Vector2 contender = (curve.Control-Vector2.UnitY*8) - curve.Begin;
                contender = contender.SafeNormalize();
                Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                    $"Comparing {best} to {contender}");
                if ((dir - contender).LengthSquared() < (dir - best).LengthSquared())
                {
                    best = contender;
                    bestindex = index;
                }
                Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                    $"{best} won");
            }
            return bestindex;
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
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                "Checking if node is exit...");
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Node",
                $"Number of rails: {Rails.Count}");
            if (Rails.Count <= (JustEntered ? 0 : 1))
            {
                ReentryTimer = ReentryTime;
                return true;
            }
            return false;
        }
    }
}
