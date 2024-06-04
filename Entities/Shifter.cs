using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste;
using Microsoft.Xna.Framework;
using System.Security.Cryptography;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    public class Shifter : Entity
    {

        internal class SpeedDistort : LiftSpeedBonus
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
                if (actor.TreatNaive)
                {
                    actor.NaiveMove(Speed * Engine.DeltaTime);
                }
                else
                {
                    actor.MoveH(Speed.X * Engine.DeltaTime);
                    actor.MoveV(Speed.Y * Engine.DeltaTime);
                }
            }

            public override Vector2 GetSpeed()
            {
                return Speed;
            }
        }

        private class ShifterTracker : Entity
        {
            public ShifterTracker From(Scene scene)
            {
                ShifterTracker ret = scene.Entities.FindFirst<ShifterTracker>();
                if (ret is null)
                    ret = new ShifterTracker();
                return ret;
            }
        }

        public Shifter(EntityData data, Vector2 offset) 
        : this(data.Position+offset, new Vector2(data.Width, data.Height), new Vector2(data.Float("SpeedX"), data.Float("SpeedY")), data.Bool("Visible")) { }

        public Shifter(Vector2 position, Vector2 size, Vector2 speed, bool Visible = true)
        {

        }
    }
}
