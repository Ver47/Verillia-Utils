using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste;
using Microsoft.Xna.Framework;
using System.Security.Cryptography;
using IL.MonoMod;
using Celeste.Mod.Entities;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    [Tracked]
    [CustomEntity("VerUtils/Shifter")]
    public class Shifter : Entity
    {

        internal class SpeedDistort : SpeedBonus
        {
            Vector2 Speed;
            bool AffectLiftBoostCap = true;
            Shifter source;

            public SpeedDistort(Vector2 speed, Shifter shifter, bool affectLiftBoostCap = true)
            {
                Speed = speed;
                source = shifter;
                AffectLiftBoostCap = affectLiftBoostCap;
            }

            public override Vector2 GetLiftSpeed(Vector2 orig)
            {
                return orig-Speed;
            }

            public override Vector2 GetLiftSpeedCapShift(Vector2 orig, Vector2 whatever)
            {
                return orig - Speed;
            }

            public override Vector2 Move(int overH, int overV)
            {
                if (actor.IsRidingAnySolidOrJumpThru()
                    || actor.TreatNaive)
                {
                    if (!Collide.Check(actor, source))
                        RemoveSelf();
                    return base.Move(overH, overV);
                }
                // consider move
                Vector2 move = Speed * Engine.DeltaTime;
                Vector2 og = move;
                Vector2 over = new Vector2(overH, overV);

                Vector2 origPos = actor.ExactPosition;
                Vector2 origOver = over;

                int SpeedSign = Math.Sign(Speed.X);
                if (SpeedSign != 0)
                {
                    if (overH != 0) //determine if the overpass actually matters
                    {
                        //remove the overpass (ensure that Speed and the overpass are of opposite direction)
                        move.X -= overH * ((Math.Sign(overH) == SpeedSign) ? 0 : 1);
                        //ensure that it doesn't get reversed
                        move.X *= SpeedSign == Math.Sign(move.X) ? 1 : 0;
                        //remove the used up overpass
                        over.X -= og.X - move.X;
                    }
                    if (SpeedSign == Directions.X_Left?
                        actor.Collider.AbsoluteLeft >= source.Collider.AbsoluteRight :
                        actor.Collider.AbsoluteRight <= source.Collider.AbsoluteLeft)
                        actor.NaiveMoveH(move.X);
                    else
                        actor.MoveH(move.X);
                    if (!Collide.Check(actor, source))
                    {
                        if (actor.Collider.AbsoluteLeft >= source.Collider.AbsoluteRight)
                        {
                            if (SpeedSign == Directions.X_Right)
                            {
                                actor.NaiveMoveToX(source.Collider.AbsoluteRight - actor.Collider.Left);
                                RemoveSelf();
                                return over;
                            }
                            over.X = origOver.X;
                            actor.NaiveMoveToX(origPos.X);
                        }
                        else if (actor.Collider.AbsoluteRight <= source.Collider.AbsoluteLeft)
                        {
                            if (SpeedSign == Directions.X_Left)
                            {
                                actor.NaiveMoveToX(source.Collider.AbsoluteLeft - actor.Collider.Right);
                                RemoveSelf();
                                return over;
                            }
                            over.X = origOver.X;
                            actor.NaiveMoveToX(origPos.X);
                        }
                        RemoveSelf();
                    }
                }
                SpeedSign = Math.Sign(Speed.Y);
                if (SpeedSign != 0)
                {
                    if (overV != 0) //determine if the overpass actually matters
                    {
                        //remove the overpass
                        move.Y -= overV * ((Math.Sign(overV) == SpeedSign) ? 0 : 1);
                        //ensure that it doesn't get reversed
                        move.Y *= SpeedSign == Math.Sign(move.Y) ? 1 : 0;
                        //remove the used up overpass
                        over.Y -= og.Y - move.Y;
                    }
                    if (SpeedSign == Directions.Y_Up ?
                        actor.Collider.AbsoluteTop >= source.Collider.AbsoluteBottom :
                        actor.Collider.AbsoluteBottom <= source.Collider.AbsoluteTop)
                        actor.NaiveMoveV(move.Y);
                    else
                        actor.MoveV(move.Y);
                    if (!Collide.Check(actor, source))
                    {
                        if (actor.Collider.AbsoluteTop >= source.Collider.AbsoluteBottom)
                        {
                            if (SpeedSign == Directions.Y_Down)
                            {
                                actor.NaiveMoveToY(source.Collider.AbsoluteBottom - actor.Collider.Top);
                                RemoveSelf();
                                return over;
                            }
                            over.Y = origOver.Y;
                            actor.NaiveMoveToY(origPos.Y);
                        }
                        else if (actor.Collider.AbsoluteBottom <= source.Collider.AbsoluteTop)
                        {
                            if (SpeedSign == Directions.Y_Up)
                            {
                                actor.NaiveMoveToY(source.Collider.AbsoluteTop - actor.Collider.Bottom);
                                RemoveSelf();
                                return over;
                            }
                            over.Y = origOver.Y;
                            actor.NaiveMoveToY(origPos.Y);
                        }
                        RemoveSelf();
                    }
                }
                return over;
            }
        }

        [Tracked]
        private class ShifterBG : Entity
        {
            public ShifterBG From(Scene scene)
            {
                ShifterBG ret = scene.Entities.FindFirst<ShifterBG>();
                if (ret is null)
                    ret = new ShifterBG();
                return ret;
            }
        }

        public Vector2 Speed;
        public bool AffectLiftBoostCap = true;
        public struct Particle
        {
            public Color color = Color.White;
            public Vector2 Position;
            public Vector2 Speed;
            public Vector2 Acceleration;
            public float RemainingLife = 1f;
            public readonly float FullLife = 1f;
            public Particle(Vector2 position, Vector2 speed, Vector2 acceleration, float life, Color color)
            {
                Position = position;
                Speed = speed;
                Acceleration = acceleration;
                FullLife = life;
                RemainingLife = FullLife;
                this.color = color;
            }
            public void Update()
            {
                Position += (Speed + (Acceleration * Acceleration)) * Engine.DeltaTime;
                Speed += Acceleration * Engine.DeltaTime;
                RemainingLife -= Engine.DeltaTime;
            }
            public void SuddenlyFade()
            {
                if (RemainingLife < FullLife * 7 / 8f)
                    RemainingLife = FullLife - RemainingLife;
                RemainingLife = Math.Min(FullLife / 8, RemainingLife);
            }
            public Color GetColor()
            {
                if (RemainingLife < 0)
                    return Color.Transparent;
                float Fade = Calc.YoYo(RemainingLife / FullLife) * 4;
                if (Fade >= 1f)
                    return color;
                Color ret = color;
                ret.A = (byte)((color.A / 255f) * Fade * 255);
                ret.A = 255;
                return ret;
            }
            public bool Fading() => (RemainingLife <= FullLife / 8);
            public void Render()
            {
                Draw.Point(Position, GetColor());
            }
        }
        public List<Particle> particles;
        public const float ParticlesPerTileSecond = 0.025f;
        public const float speedVariation = 8;
        public const float accelVariation = 8;
        public const float minLife = 0.5f;
        public const float lifeVariation = 1.5f;
        public static readonly Color[] particleColors = [Calc.HexToColor("deb754")];
        private float remainder = 0;
        private int Seed = 0;

        public Shifter(EntityData data, Vector2 offset) 
        : this(
              data.Position+offset,
              new Vector2(data.Width, data.Height),
              new Vector2(data.Float("SpeedX"), data.Float("SpeedY")),
              data.Bool("Visible"),
              data.Bool("AffectSpeedCap", true)
              ) 
        {

        }

        public Shifter(Vector2 position, Vector2 size, Vector2 speed, bool visible = true, bool affectSpeedCap = true)
        {
            Position = position;
            Collider = new Hitbox(size.X, size.Y);
            Visible = visible;
            Speed = speed;
            particles = new List<Particle>();
        }

        public override void Update()
        {
            foreach(var actor in CollideAll<Actor>())
            {
                if (actor.Components.Get<SpeedDistort>() is null)
                    actor.Add(new SpeedDistort(Speed, this, AffectLiftBoostCap));
            }
            var next = new List<Particle>();
            foreach (var p in particles)
            {
                p.Update();
                if (p.RemainingLife <= 0)
                    continue;
                if (!p.Fading() && !Collide.CheckPoint(this, p.Position))
                {
                    if (!ParticleLeave(p))
                        next.Add(p);
                }
                else
                    next.Add(p);
            }
            base.Update();
            NewParticles();
        }

        private void NewParticles()
        {
            float NewParticles = Engine.DeltaTime * ParticlesPerTileSecond * Collider.Width * Collider.Height / 64;
            NewParticles += remainder;
            int NewParticlesInt = (int)Math.Floor(NewParticles);
            remainder = NewParticles - NewParticlesInt;
            Calc.PushRandom(Seed);
            for (int i = 0; i < NewParticlesInt; i++)
            {
                Vector2 pos = Vector2.Zero;
                pos.X = Collider.AbsoluteLeft + Calc.Random.NextFloat(Collider.Right - Collider.Left);
                pos.Y = Collider.AbsoluteTop + Calc.Random.NextFloat(Collider.Bottom - Collider.Top);
                Vector2 speed = Speed;
                speed.X += Calc.Random.NextFloat(speedVariation * 2) - speedVariation;
                speed.Y += Calc.Random.NextFloat(speedVariation * 2) - speedVariation;
                Vector2 accel = Vector2.Zero;
                accel.X += Calc.Random.NextFloat(accelVariation * 2) - accelVariation;
                accel.Y += Calc.Random.NextFloat(accelVariation * 2) - accelVariation;
                float life = minLife + Calc.Random.NextFloat(lifeVariation);
                Color col = Calc.Random.Choose(particleColors);
                particles.Add(new Particle(pos, speed, accel, life, col));
            }
            Calc.PopRandom();
            Seed++;
        }

        private bool ParticleLeave(Particle p)
        {
            foreach (Shifter shifter in Scene.Tracker.GetEntities<Shifter>())
            {
                if (shifter.Speed != Speed || shifter == this)
                {
                    continue;
                }
                if (Collide.CheckPoint(shifter, p.Position))
                {
                    shifter.particles.Add(p);
                    return true;
                }
            }
            p.SuddenlyFade();
            return false;
        }

        public override void Render()
        {
            foreach (var p in particles)
                p.Render();
            base.Render();
        }
    }
}
