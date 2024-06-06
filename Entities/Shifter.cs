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

namespace Celeste.Mod.Verillia.Utils.Entities
{
    [Tracked]
    public class Shifter : Entity
    {

        internal class SpeedDistort : SpeedBonus
        {
            Vector2 Speed;
            Shifter source;

            public SpeedDistort(Vector2 speed, Shifter shifter)
            {
                Speed = speed;
                source = shifter;
            }

            public override void Update()
            {
                base.Update();
                if (!Collide.Check(actor, source))
                {
                    RemoveSelf();
                }
            }

            public override Vector2 GetLiftSpeed(Vector2 orig)
            {
                return orig-Speed;
            }

            public override void Move(int overH, int overV)
            {
                base.Move(overH, overV);
                if (actor.OnGround())
                    return;
                if (actor.TreatNaive)
                    return;
                if (!Collide.Check(actor, source))
                    return;
                // consider move
                Vector2 move = Speed * Engine.DeltaTime;
                move.X -= overH * Math.Sign(Math.Sign(Speed.X) - Math.Sign(overH));
                move.Y -= overV * Math.Sign(Math.Sign(Speed.Y) - Math.Sign(overV));
                // overpass exceeding speed will just be zero (cannot be different signs.)
                move.X *= Math.Sign(Math.Sign(Speed.X) + Math.Sign(move.X));
                move.Y *= Math.Sign(Math.Sign(Speed.Y) + Math.Sign(move.Y));
                actor.MoveH(move.X);
                if (!Collide.Check(actor, source))
                {
                    if (actor.Position.X + actor.Collider.Left >= source.Position.X + source.Collider.Right)
                    {
                        actor.MoveToX(source.Position.X + source.Collider.Right - actor.Collider.Left);
                    }
                    else
                    {
                        actor.MoveToX(source.Position.X + source.Collider.Left - actor.Collider.Right);
                    }
                    return;
                }
                actor.MoveV(move.Y);
                if (!Collide.Check(actor, source))
                    if (actor.Position.Y + actor.Collider.Top >= source.Position.Y + source.Collider.Bottom)
                    {
                        actor.MoveToY(source.Position.Y + source.Collider.Bottom - actor.Collider.Top);
                    }
                    else
                    {
                        actor.MoveToY(source.Position.Y + source.Collider.Top - actor.Collider.Bottom);
                    }
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
        public struct Particle
        {
            public Color color;
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
                return ret;
            }
            public bool Fading() => (RemainingLife <= FullLife / 8);
            public void Render()
            {
                Draw.Point(Position, GetColor());
            }
        }
        public List<Particle> particles;

        public Shifter(EntityData data, Vector2 offset) 
        : this(data.Position+offset, new Vector2(data.Width, data.Height), new Vector2(data.Float("SpeedX"), data.Float("SpeedY")), data.Bool("Visible")) { }

        public Shifter(Vector2 position, Vector2 size, Vector2 speed, bool visible = true)
        {
            Position = position;
            collider = new Hitbox(size.X, size.Y);
            Visible = visible;
            Speed = speed;
        }

        public override void Update()
        {
            foreach(var actor in CollideAll<Actor>())
            {
                if (actor.Components.Get<SpeedDistort>() is null)
                    actor.Add(new SpeedDistort(Speed, this));
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
