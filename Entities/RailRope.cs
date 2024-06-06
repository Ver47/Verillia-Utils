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

        //Determines if the player's invincible when travelling through.
        public readonly bool InvincibleOnTravel = true;
        public bool Background { get; private set; }

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

        //Rope details
        public SimpleCurve Rope { get; private set; }
        public Vector2 CurveControl { get; private set; }

        //Rope shine
        private const int shineRadius = 3;
        private const int shinefadeRadius = 10;
        private const float minShineLength = 5f;
        private const float maxShineLength = 7f;
        private SineWave shineposition;
        private SineWave shineAlpha;
        private VertexLight shine;
        private BloomPoint shinebloom;

        public RailRope(EntityData data, Vector2 offset) 
        : this(
              data.Position + offset,
              data.NodesOffset(offset)[0],
              data.Int("slough", 0),
              data.Bool("invincible", true),
              data.Bool("bg", false),
              data.Int("priority", 0)
              ) { }

        public RailRope(Vector2 positionA, Vector2 positionB, float slough, bool Invincible = true, bool Bg = false, int priority = 0) : base(positionA)
        {
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                "Constructing" );

            //Meta stuff
            Background = Bg;
            InvincibleOnTravel = Invincible;
            Depth = Background ?
                VerUtils.Depths.RailBooster_Rail_BG :
                VerUtils.Depths.RailBooster_Rail_FG;
            Priority = priority;

            //Set up the catenary
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                "Setting catenary...");
            Vector2 p0 = (positionA.X < positionB.X) ?
                positionA : positionB;
            Vector2 p1 = (positionA.X < positionB.X) ?
                positionB : positionA;
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Right point is at: {p1}");
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Left point is at: {p0}");

            CurveControl = ((p0 + p1) / 2) + (Vector2.UnitY * slough);
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Middle control point is at: {CurveControl}");

            Rope = new SimpleCurve(p0, p1, CurveControl);
            Position = p0;

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
            Calc.PushRandom((int)(p0.LengthSquared()/length));
            float WobbleFrequency = Calc.Random.Range(MinWobbleFrequency, MaxWobbleFrequency);
            Logger.Log(LogLevel.Debug, "VerUtils/RailBooster-Rope",
                $"Wobbling at {WobbleFrequency}Hz");
            Add(Wobble = new SineWave(WobbleFrequency));
            Wobble.Randomize();
            TransitionListener trans = new TransitionListener();
            trans.OnInBegin = () => { SineActive(false); };
            trans.OnInEnd = () => { SineActive(true); };
            trans.OnOutBegin = () => { SineActive(false); };
            Add(trans);
            Tag = Tag.WithTag(Tags.TransitionUpdate);

            //Add the shine
            Add(shinebloom = new BloomPoint(1, (shineRadius + shinefadeRadius) / 2));
            Add(shine = new VertexLight(Color.White, 0, shineRadius, shinefadeRadius));
            Add(shineAlpha = new SineWave(1 / Calc.Random.Range(minShineLength, maxShineLength), (float)(Calc.Random.NextDouble() * 6.2831854820251465)));
            shineAlpha.OnUpdate = UpdateShineAlpha;
            UpdateShineAlpha(shineAlpha.Value);
            Add(shineposition = new SineWave(1 / Calc.Random.Range(minShineLength, maxShineLength), (float)(Calc.Random.NextDouble() * 6.2831854820251465)));
            shineposition.OnUpdate = UpdateShinePos;
            UpdateShinePos(shineposition.Value);

            Calc.PopRandom();
            Add(new MirrorReflection());
        }

        private void SineActive(bool set)
        {
            Wobble.Active = set;
            shineAlpha.Active = set;
            shineposition.Active = set;
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

        public SimpleCurve RopeWithWobble()
        {
            return Rope with
            {
                Control =
                    CurveControl
                    + (Vector2.UnitY * ((Wobble.Value + 1) * (WobbleOffset / 2)))
            };
        }

        public override void Render()
        {
            base.Render();
            var BaseRope = RopeWithWobble();
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
            Position = Rope.Begin;
            base.Update();
        }

        private void UpdateShineAlpha(float sine)
        {
            shine.Alpha = 1-Math.Abs(sine);
            if (!Background)
                shine.InSolidAlphaMultiplier = 1;
            shinebloom.Alpha = shine.Alpha * shine.InSolidAlphaMultiplier;
        }
        private void UpdateShinePos(float sine)
        {
            shine.Position = RopeWithWobble().GetPoint((sine + 1) / 2) + Vector2.UnitY - Position;
            if (!Background)
                shine.LastNonSolidPosition = Position + shine.Position;
            shinebloom.Position = shine.LastNonSolidPosition - Position;
        }
    }
}
