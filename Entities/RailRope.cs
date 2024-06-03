using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    [CustomEntity("VerUtils/RailBooster-Rail")]
    [Tracked]
    public class RailRope : Entity
    {
        //Rope details
        public RailBooster endA;
        public int indexA
        {
            get
            {
                return endA.Rails.ToList().IndexOf(this);
            }
            private set { }
        }
        public RailBooster endB;
        public int indexB
        {
            get
            {
                return endB.Rails.ToList().IndexOf(this);
            }
            private set { }
        }
        public int PointCount { get; private set; }
        private int slough;

        //Determines if the player's invincible when travelling through.
        public readonly bool InvincibleOnTravel = true;

        // rope setup details
        const int LengthCheckBaseRes = 16;
        const int PointCountStep = 8;
        const float MaxPointDistance = 16f;

        //Priority allows default thing
        public readonly int Priority = 0;

        //Rendered Wobble
        internal SineWave Wobble;
        private const float MinWobbleFrequency = 1f;
        private const float MaxWobbleFrequency = 1.5f;
        private const float WobbleOffset = 4f;

        //Rope rendering specs
        public static readonly Color RopeColor = Calc.HexToColor("a986d3");
        public static readonly Color ShadowColor = Calc.HexToColor("563b85");
        public static readonly Color OutlineColor = Calc.HexToColor("e6c1ec");
        public const float RopeThickness = 2f;

        public SimpleCurve Rope { get; private set; }
        public Vector2 CurveMiddle { get; private set; }

        public RailRope(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                "Constructing" );

            //Meta stuff
            Vector2 position = data.Position + offset;
            bool Background = data.Bool("bg", false);
            InvincibleOnTravel = data.Bool("invincible", true);
            Depth = Background ?
                VerUtils.Depths.RailBooster_Rail_BG :
                VerUtils.Depths.RailBooster_Rail_FG;
            Priority = data.Int("priority", 0);
            Position = position;

            slough = data.Int("slough", 0);

            //Set up the catenary
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                "Setting catenary...");
            Vector2 p0 = (position.X < data.NodesOffset(offset)[0].X) ?
                position : data.NodesOffset(offset)[0];
            Vector2 p1 = (position.X < data.NodesOffset(offset)[0].X) ?
                data.NodesOffset(offset)[0] : position;
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Right point is at: {p1}");
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Left point is at: {p0}");

            CurveMiddle = ((p0 + p1) / 2) + (Vector2.UnitY * data.Int("slough", 0));
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Middle control point is at: {CurveMiddle}");

            Rope = new SimpleCurve(p0, p1, CurveMiddle);

            PointCount = LengthCheckBaseRes;
            float length = Rope.GetLengthParametric(PointCount);
            while (length/PointCount > MaxPointDistance)
            {
                PointCount += PointCountStep;
                length = Rope.GetLengthParametric(PointCount);
            }
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Length is set to: {length}");
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Curve resolution set to: {PointCount}");

            //Initialize the wobble
            //How do I make two seperate wobbles sync-
            Calc.PushRandom((p0*length).GetHashCode());
            float WobbleFrequency = Calc.Random.Range(MinWobbleFrequency, MaxWobbleFrequency);
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Wobbling at {WobbleFrequency}Hz");
            Add(Wobble = new SineWave(WobbleFrequency));
            Wobble.Randomize();
            Calc.PopRandom();
            TransitionListener trans = new TransitionListener();
            trans.OnInBegin = WobbleSync;
            Add(trans);

            Add(new MirrorReflection());
        }

        private void WobbleSync()
        {
            foreach (var e in Scene.Tracker.GetEntities<RailRope>())
            {
                var rope = e as RailRope;
                if (rope != this
                    && rope.Rope.Begin == Rope.Begin
                    && rope.Rope.End == Rope.End
                    && rope.Rope.Control == Rope.Control)
                {
                    rope.Wobble.Counter = Wobble.Counter;
                }
            }
        }

        public override void Awake(Scene scene)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                "Getting left booster for position: " + Rope.Begin.ToString());
            // Attaching the rope, simple as that
            RailBooster referral;
            if (endA is null)
            {
                referral = scene.Tracker.GetNearestEntity<RailBooster>(Rope.Begin);
                if (referral != null && referral.Position == Rope.Begin)
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                        $"Detected node at position: {referral.Position}");
                    endA = referral;
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                        $"None found.");
                    scene.Add(endA = new RailBooster(Rope.Begin, false, false));
                    endA.Connect(scene);
                }
                endA.AddRail(this);
            }
            else
                Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                        $"Oh wait... there already is one, lol.");

            Logger.Log(LogLevel.Debug, "VerUtils-RailBooster_Rope",
                "Getting right booster for position: " + Rope.End.ToString());
            if (endB is null)
            {
                referral = scene.Tracker.GetNearestEntity<RailBooster>(Rope.End);
                if (referral != null && referral.Position == Rope.End)
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                        $"Detected node at position: {referral.Position}");
                    endB = referral;
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                        $"None found.");
                    scene.Add(endB = new RailBooster(Rope.End, false, false));
                    endB.Connect(scene);
                }
                endB.AddRail(this);
            }
            else
                Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                    $"Oh wait... there already is one, lol.");
            base.Awake(scene); 
        }

        public SimpleCurve getPathFrom(Vector2 origin)
        {
            if ((origin - Rope.Begin).LengthSquared() < (origin - Rope.End).LengthSquared())
                return Rope;
            return new SimpleCurve(Rope.End, Rope.Begin, Rope.Control);
        }

        public SimpleCurve giveWobbleTo(SimpleCurve curve)
        {
            return curve with
            {
                Control =
                    CurveMiddle
                    + (Vector2.UnitY * ((Wobble.Value + 1) * (WobbleOffset / 2)))
            };
        }

        public override void Render()
        {
            base.Render();
            var BaseRope = giveWobbleTo(Rope);
            //details
            for (int i = -1; i < 2; i += 2)
            {
                for (int j = 0; j < 2; j++)
                {
                    var RenderRope = new SimpleCurve(
                        BaseRope.Begin + (Vector2.UnitX * i) + (Vector2.UnitY * j),
                        BaseRope.End + (Vector2.UnitX * i) + (Vector2.UnitY * j),
                        BaseRope.Control + (Vector2.UnitX * i) + (Vector2.UnitY * j));
                    RenderRope.RenderBetter(
                        OutlineColor,
                        PointCount,
                        RopeThickness
                        );
                }
            }
            var Colors = new Color[]{OutlineColor, OutlineColor, ShadowColor, RopeColor};
            var Positions = new int[] { 2, -1, 1, 0 };
            for (int i=0; i<Colors.Length; i++)
            {
                var offset = Positions[i];
                var RenderRope = new SimpleCurve(
                    BaseRope.Begin + (Vector2.UnitY * offset),
                    BaseRope.End + (Vector2.UnitY * offset),
                    BaseRope.Control + (Vector2.UnitY * offset));
                RenderRope.RenderBetter(
                    Colors[i],
                    PointCount,
                    RopeThickness
                    );
            }
        }

        public override void Update()
        {
            base.Update();
        }

        static public float getWobbleStrength(float f) =>
            (float)Math.Sin(f * Math.PI);
    }
}
