using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using System.Reflection;


namespace Celeste.Mod.Verillia.Utils
{
    public class SpeedBonus : ComplexComponent
    {
        protected Actor actor => EntityAs<Actor>();
        protected Player player => EntityAs<Player>();
        protected Vector2 ActorSpeed
        {
            get
            {
                if (!SpeedGettable)
                    return Vector2.Zero;
                return (Vector2)actor.GetValueOfMember("Speed",
                    BindingFlags.Instance | BindingFlags.Public);
            }
            set
            {
                actor.SetValueOfMember("Speed", value,
                    BindingFlags.Instance | BindingFlags.Public);
            }
        }
        protected bool SpeedGettable
        {
            get
            {
                var info = actor.GetType().GetFieldOrPropertyInheritance("Speed",
                    BindingFlags.Instance | BindingFlags.Public);
                if (info == null)
                    return false;
                if (info is PropertyInfo prop)
                {
                    return prop.CanRead;
                }
                return true;
            }
        }
        protected bool SpeedSettable
        {
            get
            {
                var info = actor.GetType().GetFieldOrPropertyInheritance("Speed",
                    BindingFlags.Instance | BindingFlags.Public);
                if (info == null)
                    return false;
                if (info is PropertyInfo prop)
                {
                    return prop.CanWrite;
                }
                return true;
            }
        }

        internal SpeedBonus()
            : base(true, false)
        {
        }

        public sealed override void Update()
        {
            base.Update();
        }

        public sealed override void Render()
        {
            Visible = false;
            base.Render();
        }

        public sealed override void PreUpdate()
        {
            base.PreUpdate();
        }

        public sealed override void PostUpdate()
        {
            base.PostUpdate();
        }

        public override void Removed(Entity entity)
        {
            base.Removed(entity);
        }

        internal void Move()
        {
            var overpass = actor.GetCounterMovement();
            //Record previous overpass
            int H = overpass.GoalH;
            int V = overpass.GoalV;
            var next = Move(overpass.H, overpass.V);
            //Add deltaoverpass
            overpass.GoalH += H - (int)Math.Round(next.X);
            overpass.GoalV += V - (int)Math.Round(next.Y);
        }

        public virtual Vector2 GetLiftSpeed(Vector2 orig)
        {
            return orig;
        }

        public virtual Vector2 GetLiftSpeedCapShift(Vector2 orig, Vector2 liftspeed)
        {
            return orig;
        }

        public virtual Vector2 Move(int overH, int overV) { return new Vector2(overH, overV); }
    }
}
