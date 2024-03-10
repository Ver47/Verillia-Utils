using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    //Thanks to Viv for handling the catenary calculus stuff!
    [CustomEntity("VerUtils/RailBooster-Rail")]
    public class RailRope : Entity
    {
        //Rope details
        public RailBooster endA;
        public int indexA
        {
            get
            {
                return endA.Rails.IndexOf(this);
            }
            private set { }
        }
        public RailBooster endB;
        public int indexB
        {
            get
            {
                return endB.Rails.IndexOf(this);
            }
            private set { }
        }
        public int PointCount { get; private set; }

        //Determines if the player's invincible when travelling through.
        public readonly bool InvincibleOnTravel = true;

        // rope setup details
        const int LengthCheckBaseRes = 16;
        const int PointCountStep = 8;
        const float MaxPointDistance = 16f;

        //Priority allows default thing
        public readonly int Priority = 0;

        //Rendered Wobble
        private SineWave Wobble;
        private const float MinWobbleFrequency = 1f;
        private const float MaxWobbleFrequency = 1.5f;
        private const float WobbleOffset = 4f;

        //Rope rendering specs
        public static readonly Color RopeColor = Calc.HexToColor("FFFFFF");
        public const float RopeThickness = 4f;

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

            //Set up the catenary
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                "Setting catenary...");
            Vector2 p0 = (position.X < data.NodesOffset(offset)[0].X) ?
                position : data.NodesOffset(offset)[0];
            Vector2 p1 = (position.X < data.NodesOffset(offset)[0].X) ?
                data.NodesOffset(offset)[0] : position;
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Right point is at: {p1}");
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Left point is at: {p0}");

            CurveMiddle = ((p0 + p1) / 2) + (Vector2.UnitY * data.Int("slough", 0));
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Middle control point is at: {CurveMiddle}");

            Rope = new SimpleCurve(p0, CurveMiddle, p1);

            PointCount = LengthCheckBaseRes;
            float length = Rope.GetLengthParametric(PointCount);
            while (length/PointCount > MaxPointDistance)
            {
                PointCount += PointCountStep;
                length = Rope.GetLengthParametric(PointCount);
            }
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Length is set to: {length}");
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Curve resolution set to: {PointCount}");

            //Initialize the wobble
            //Not sure how to make to equal catenaries seamlessly sync
            //Especially across two levels
            //This is the best I could think of
            Calc.PushRandom((p0*length).GetHashCode());
            float WobbleFrequency = Calc.Random.Range(MinWobbleFrequency, MaxWobbleFrequency);
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Wobbling at {WobbleFrequency}Hz");
            Add(Wobble = new SineWave(WobbleFrequency));
            Wobble.Randomize();
            Calc.PopRandom();
        }

        public override void Awake(Scene scene)
        {
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                "Getting left booster for position: " + Rope.Begin.ToString());
            // Attaching the rope, simple as that
            RailBooster referral = scene.Tracker.GetNearestEntity<RailBooster>(Rope.Begin);
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Detected node at position: {referral.Position}");
            if (referral != null && referral.Position == Rope.Begin)
                endA = referral;
            else
                scene.Add(endA = new RailBooster(Rope.Begin, false, false));
            endA.AddRail(this);

            Logger.Log(LogLevel.Verbose, "VerUtils-RailBooster_Rope",
                "Getting right booster for position: " + Rope.End.ToString());
            referral = scene.Tracker.GetNearestEntity<RailBooster>(Rope.End);
            Logger.Log(LogLevel.Verbose, "VerUtils/RailBooster-Rope",
                $"Detected node at position: {referral.Position}");
            if (referral != null && referral.Position == Rope.End)
                endB = referral;
            else
                scene.Add(endB = new RailBooster(Rope.End, false, false));
            endB.AddRail(this);
            base.Awake(scene); 
        }

        public SimpleCurve getPathFrom(Vector2 origin)
        {
            if ((origin - Rope.Begin).LengthSquared() < (origin - Rope.End).LengthSquared())
                return Rope;
            return new SimpleCurve(Rope.End, Rope.Control, Rope.Begin);
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
            var RenderRope = giveWobbleTo(Rope);
            RenderRope.Render(
                RopeColor,
                PointCount,
                RopeThickness
                );
        }

        public override void Update()
        {
            base.Update();
        }

        static public float getWobbleStrength(float f) =>
            (float)Math.Sin(f * Math.PI);
    }
}
