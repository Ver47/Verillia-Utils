using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste;
using Microsoft.Xna.Framework;
using Celeste.Mod.Verillia.Utils.Entities;

namespace Celeste.Mod.Verillia.Utils
{
    public class VerilliaUtilsPlayerExt : Component
    {
        public Dictionary<string, int> MovementModes;

        //Railbooster Stuff
        public Vector2[] RailBoosterPath = new Vector2[1];
        public static Vector2 BoosterPlayerOffset = new Vector2(0f, -2f);
        public const float BoosterTravelSpeed = 80f;
        public static int RailBoostState;

        public VerilliaUtilsPlayerExt(bool active = true, bool visible = true) : base(active, visible)
        {
        }

        public override void Update() { }

        public int RailBoost(Player player) {
            return RailBoostState;
        }

        public IEnumerator RailBoostCoroutine(Player player)
        {
            Vector2 direction = Vector2.Zero;
            int RailIndex = -1;
            while (true)
            {
                RailBooster Node = Scene.Tracker.GetNearestEntity<RailBooster>(player.Position - BoosterPlayerOffset);
                if (Node.isExit())
                {
                    player.Speed = direction;
                    yield break;
                }
                if (Node.getDefault(RailIndex) == RailIndex)
                {
                    yield return 0.1f;
                    int LastX = Input.MoveX;
                    int LastY = Input.MoveY;
                    float Timer = Node.getTimeLimit();
                    while (Timer > 0)
                    {
                        yield return null;
                        Timer -= Engine.DeltaTime;
                        if (Input.MoveX != LastX && Input.MoveX != 0)
                            Node.CycleX(RailIndex, Input.MoveX > 0);
                        if (Input.MoveY != LastY && Input.MoveY != 0)
                            Node.CycleY(RailIndex, Input.MoveY > 0);
                        LastX = Input.MoveX;
                        LastY = Input.MoveY;
                    }
                }
                else
                {
                    RailIndex = Node.getDefault(RailIndex);
                }
                RailBoosterPath = Node.Rails[RailIndex].points;
                foreach (Vector2 nextpoint in RailBoosterPath)
                {
                    Vector2 nextPos = nextpoint + BoosterPlayerOffset;
                    direction = nextPos - player.Position;
                    direction.Normalize();
                    direction *= BoosterTravelSpeed;
                    while (player.Position != nextPos)
                    {
                        player.MoveTowardsX(nextPos.X, direction.X);
                        player.MoveTowardsY(nextPos.Y, direction.Y);
                        yield return null;
                    }
                }
            }
        }
    }
}
