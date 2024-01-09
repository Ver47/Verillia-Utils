using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Verillia.Utils.Entities;

namespace Celeste.Mod.Verillia.Utils
{
    public class VerilliaUtilsPlayerExt : Component
    {

        //Railbooster Stuff
        public const float BoosterTravelSpeed = 80f;
        public int RailBoostState;
        private bool InState;

        public VerilliaUtilsPlayerExt(bool active = true, bool visible = true) : base(active, visible)
        {
        }

        public override void Update() { }

        public void GenericStartState(Player player)
        {
            InState= true;
        }

        public int RailBoostUpdate(Player player) {
            player.Collidable = false;
            if (!InState)
            {
                player.Collidable = true;
                return Player.StNormal;
            }
            return RailBoostState;
        }

        public IEnumerator RailBoostCoroutine(Player player)
        {
            // Start of by getting the booster you are going through
            RailBooster Node = Scene.Tracker.GetNearestEntity<RailBooster>(player.Position - RailBooster.PlayerOffset);
            int RailIndex = -1;
            Vector2 newPos;
            Vector2 velocity;
            while (player.Position != Node.Position + RailBooster.PlayerOffset)
            {
                yield return null;
                velocity = (Node.Position + RailBooster.PlayerOffset) - player.Position;
                velocity.Normalize();
                velocity *= RailBooster.CenterSuckSpeed*Engine.DeltaTime;
                player.NaiveMove(player.Position+velocity);
            }
            //Defaults to zero, use this for some fancy alignment tech.
            velocity = Vector2.Zero;
            while (true)
            {
                // Immediately end the action on exit
                // please ensure that there is a way to return to StNormal
                if (Node.isExit())
                {
                    velocity.SafeNormalize();
                    velocity *= BoosterTravelSpeed;
                    player.Speed = velocity;
                    InState = false;
                    yield break;
                }
                // Returning the given integer means that the player would have to choose manually
                // maybe replace that with a pass through check or something?
                if (Node.getDefault(RailIndex) == RailIndex)
                {
                    //Ensure player can't backtrack
                    int EntryIndex = RailIndex;
                    // Method of deciding if the directions are inputted or not.
                    // There must be something better than this...
                    int LastX = Input.MoveX;
                    int LastY = Input.MoveY;
                    //Player gets limited decision time before they continue
                    //Time decided is based on the amount of paths
                    float Timer = Node.getTimeLimit();
                    while (Timer > 0)
                    {
                        //Cycle logic
                        if (Input.MoveX != LastX && Input.MoveX != 0)
                        {
                            RailIndex = Node.CycleX(RailIndex, Input.MoveX > 0);
                            if (RailIndex == EntryIndex)
                                RailIndex = Node.CycleX(RailIndex, Input.MoveX > 0);
                        }
                        if (Input.MoveY != LastY && Input.MoveY != 0)
                        {
                            Node.CycleY(RailIndex, Input.MoveY > 0);
                            if (RailIndex == EntryIndex)
                                RailIndex = Node.CycleY(RailIndex, Input.MoveY > 0);
                        }
                        LastX = Input.MoveX;
                        LastY = Input.MoveY;
                        yield return null;
                        Timer -= Engine.DeltaTime;
                    }
                }
                else
                {
                    // Go to the default if autodecided
                    RailIndex = Node.getDefault(RailIndex);
                }
                //Stun frame (c:)
                yield return null;
                // bunch of movement logic
                Vector2[] RailBoosterPath = Node.Rails[RailIndex].points;
                // Define required move distance for the frame
                float MoveDistance = BoosterTravelSpeed * Engine.DeltaTime;
                foreach (Vector2 nextpoint in RailBoosterPath)
                {
                    Vector2 nextPos = nextpoint + RailBooster.PlayerOffset;
                    while (player.Position != nextPos)
                    {
                        //If going to the vertex doesn't satisfy the distance quota...
                        if (MoveDistance > (player.Position - nextPos).Length())
                        {
                            //Go to the next vertex and continue going to the one after
                            MoveDistance -= (player.Position - nextPos).Length();
                            player.NaiveMove(nextPos);
                            break;
                        }
                        // Essentially this determines the next player position
                        velocity = nextPos - player.Position;
                        velocity.Normalize();
                        velocity *= (float)MoveDistance;
                        newPos = player.Position + velocity;
                        player.NaiveMove(newPos);
                        //Proceed to next frame
                        //Note that the new distance quota only gets set here
                        yield return null;
                        MoveDistance = BoosterTravelSpeed * Engine.DeltaTime;
                    }
                }
                //Get the next booster
                Node = Scene.Tracker.GetNearestEntity<RailBooster>(player.Position - RailBooster.PlayerOffset);
            }
        }
    }
}
