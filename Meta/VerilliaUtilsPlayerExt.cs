using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Verillia.Utils.Entities;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.Verillia.Utils
{
    public class VerilliaUtilsPlayerExt : Component
    {
        #region META

        public bool Invincible = false;
        public bool Dodging
        {
            get { return player.StateMachine.State == Player.StDash || Dodging; }
            set { Dodging = value; }
        }
        public bool Aerodynamic = false;

        private Player player => EntityAs<Player>();

        internal VerilliaUtilsPlayerExt(bool active = true, bool visible = true)
            : base(active, visible)
        {

        }

        public override void Added(Entity entity)
        {
            base.Added(entity);
        }
        public override void Removed(Entity entity)
        {
            Logger.Log(LogLevel.Warn, "VerUtils/PlayerExtension",
                "Verillia Utils Player Extension has been removed. " +
                "Removing this may pose some risks. Be wary.");
            base.Removed(entity);
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
        }

        public override void EntityRemoved(Scene scene)
        {
            base.EntityRemoved(scene);
        }

        #region Render
        public void RenderBelow()
        {

        }

        public void RenderAbove()
        {

        }
        #endregion

        #region Update
        public void PreUpdate()
        {

        }

        public void PostUpdate()
        {

        }
        #endregion
        #endregion

        #region HOOKS
        #endregion

        #region STATES

        internal void InitializeStates()
        {
            Logger.Log(LogLevel.Info, "VerUtils/PlayerExtension",
                "Setting up states.");
            StRailBoost = player.StateMachine.AddState(
                "VerUtils-RailBoost",
                RailBoostUpdate,
                RailBoostCoroutine,
                RailBoostBegin,
                RailBoostEnd
                );
        }

        #region Railboost
        public const float RailBoosterTravelSpeed = 120f;
        public const float RailBoosterSpitSpeed = 200f;

        public const float RailBoosterSpitVBoost = -90f;
        public const float RailBoosterVBoostTimer = 0.1f;
        public const float RailBoosterVBoostReq = 120f; //30f should be less than the normal fall speed

        public const float RailBoosterSpitHBoost = 100f;
        public const float RailBoosterHBoostReq = 160f; //absolute must be higher than this

        public int StRailBoost { get; internal set; }
        public static readonly Vector2 BoosterRenderOffset = new(0f, -2f);

        public void RailBoostBegin()
        {
            //Disable collision
            player.TreatNaive = true;
        }

        public int RailBoostUpdate() => StRailBoost;

        public void RailBoostEnd()
        {
            //Reenable collision
            player.TreatNaive = false;
        }

        public IEnumerator RailBoostCoroutine()
        {
            Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                "Starting railboost");
            // Start of by getting the booster you are going through
            RailBooster Node = Scene.Tracker.GetNearestEntity<RailBooster>(
                player.Collider.Center + player.ExactPosition
                );
            int RailIndex = -1;
            //Defaults to player speed, helps on instant choice and spit tech
            Vector2 velocity = player.Speed;
            Facings Heading;
            if (velocity.X == 0)
                Heading = player.Facing;
            else
                Heading = velocity.X > 0 ? Facings.Right : Facings.Left;
            //the player having speed on suck is going to be annoying.
            player.Speed = Vector2.Zero;
            float Timer = RailBooster.CenterSuckTime;
            Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                $"Going to entry node at {Node.Position}");
            Vector2 playerEnterPosition = player.ExactPosition;
            while (Timer > 0)
            {
                if (Input.DashPressed)
                    break;
                yield return null;
                player.NaiveMove(
                    Vector2.Lerp(
                        Node.Center - player.Collider.Center,
                        playerEnterPosition,
                        Timer / RailBooster.CenterSuckTime
                        )
                    - player.ExactPosition);
                Timer -= Engine.DeltaTime;
            }
            bool justEntered = true;
            //Lock the player unto the railbooster
            player.NaiveMove(
                (Node.Center - player.Collider.Center)
                - player.ExactPosition
                );
            //Have it on endless loop until broken
            while (true)
            {
                Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                    $"Railbooster node has {Node.Rails.Count} options.");
                // Immediately end the action on exit
                // please ensure that there is a way to return to StNormal
                if (Node.Exit(justEntered))
                {
                    Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                        "Exiting railboost");
                    player.Facing = Heading;
                    velocity = velocity.SafeNormalize(RailBoosterSpitSpeed);
                    if (Math.Abs(velocity.X) >= RailBoosterHBoostReq)
                    {
                        Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtenion",
                            $"Reached HBoost threshold with {velocity.X}");
                        velocity.X += Math.Sign(velocity.X) * RailBoosterSpitHBoost;
                    }
                    if (velocity.Y <= RailBoosterVBoostReq)
                    {
                        Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtenion",
                            $"Reached VBoost threshold with {velocity.Y}");
                        velocity.Y += RailBoosterSpitVBoost;
                        player.AutoJump = true;
                        player.AutoJumpTimer = RailBoosterVBoostTimer;
                        player.varJumpTimer = RailBoosterVBoostTimer * 2;
                        //Unsure if I should make RailBooster Launches a tech or not...
                        player.varJumpSpeed = velocity.Y > RailBoosterSpitVBoost ?
                            velocity.Y : RailBoosterSpitVBoost;
                    }
                    player.Speed = velocity;
                    player.StateMachine.State = Player.StNormal;
                    player.launched = true;
                    Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                        $"Launched at speed {player.Speed}");
                    yield break;
                }
                justEntered = false;
                // Returning the given integer means that the player would have to choose manually
                // maybe replace that with a pass through check or something?
                Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                    "Choosing rail");
                if (Node.getDefault(RailIndex) == RailIndex)
                {
                    //Ensure player can't backtrack
                    int EntryIndex = RailIndex;
                    // Get the aim direction, on the case of instant use
                    Vector2 BoosterAim = Input.Aim.Value == Vector2.Zero ?
                        Vector2.UnitX * (float)Heading :
                        Input.Aim.Value.SafeNormalize();
                    int NewIndex = Node.getClosestToDirection(BoosterAim);
                    if (NewIndex != EntryIndex)
                        RailIndex = NewIndex;
                    // Setup for cycle logic
                    BoosterAim = Input.Aim.Value == Vector2.Zero ?
                        Vector2.Zero :
                        Input.GetAimVector(Heading);
                    int LastX = Math.Sign(BoosterAim.X);
                    int LastY = Math.Sign(BoosterAim.Y);
                    //Player gets limited decision time before they continue
                    //Time decided is based on the amount of paths
                    //(Unless the booster is instant)
                    Timer = Node.getTimeLimit();
                    while (Timer > 0)
                    {
                        if (Input.DashPressed)
                            break;
                        //Cycle logic
                        BoosterAim = Input.Aim.Value == Vector2.Zero ?
                            Vector2.Zero :
                            Input.GetAimVector(Heading);
                        //Only cycles when Jump is held
                        if (Input.Jump.Check) {
                            // for fine tuning by keyboard players, do cycle logic
                            int LastIndex = RailIndex;
                            if (Math.Sign(BoosterAim.X) != LastX && Math.Sign(BoosterAim.X) != 0)
                            {
                                RailIndex = Node.CycleX(RailIndex, Math.Sign(BoosterAim.X) > 0);
                                if (RailIndex == EntryIndex)
                                    RailIndex = Node.CycleX(RailIndex, Math.Sign(BoosterAim.X) > 0);
                            }
                            if (Math.Sign(BoosterAim.Y) != LastY && Math.Sign(BoosterAim.Y) != 0
                                && LastIndex != RailIndex)
                            {
                                Node.CycleY(RailIndex, Math.Sign(BoosterAim.Y) > 0);
                                if (RailIndex == EntryIndex)
                                    RailIndex = Node.CycleY(RailIndex, Math.Sign(BoosterAim.Y) > 0);
                            }
                        }
                        //On Default
                        //Aim the desired path based on Input.Aim
                        else if (Input.Aim.Value != Vector2.Zero)
                        {
                            BoosterAim = Input.Aim.Value;
                            BoosterAim.Normalize();
                            NewIndex = Node.getClosestToDirection(BoosterAim);
                            if (NewIndex != EntryIndex)
                                RailIndex = NewIndex;
                        }
                        //More cycle logic
                        LastX = Math.Sign(BoosterAim.X);
                        LastY = Math.Sign(BoosterAim.Y);
                        yield return null;
                        Timer -= Engine.DeltaTime;
                    }
                }
                else
                {
                    // Go to the default if autodecided
                    RailIndex = Node.getDefault(RailIndex);
                }
                Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                    $"Travelling through rail #{RailIndex}");
                RailRope Rail = Node.Rails[RailIndex];
                // Whether or not the player is invincible is based on the rail.
                Invincible = Rail.InvincibleOnTravel;
                // bunch of movement logic for rails
                SimpleCurve RailBoosterPath = Rail.getPathFrom(Node.Position);
                float PathLength =
                    RailBoosterPath.GetLengthParametric(Rail.PointCount);
                Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                    $"Rail details: {RailBoosterPath}");
                float AmountTravelled = 0f;
                // Move through the rail
                while (AmountTravelled < 1f)
                {
                    //Player Position gets set
                    player.NaiveMove
                    (
                        Rail.giveWobbleTo(RailBoosterPath)
                        .GetPoint(AmountTravelled) - player.Collider.Center
                        - player.ExactPosition
                    );
                    //Move to next point.
                    AmountTravelled +=
                        (RailBoosterTravelSpeed * Engine.DeltaTime) / PathLength;
                    //goto next frame
                    yield return null;
                }
                //Put the player at the end of the rail.
                player.NaiveMove
                (
                    RailBoosterPath.End - player.Collider.Center
                    - player.ExactPosition
                );
                // Set player velocity based on the travel speed
                velocity = RailBoosterPath.End - RailBoosterPath.Control;
                velocity.SafeNormalize(RailBoosterTravelSpeed);
                // set next heading.
                if (velocity.X != 0)
                    Heading = velocity.X > 0 ? Facings.Right : Facings.Left;
                Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                    $"Ended rail at {player.Position + player.Collider.Center}");
                //Get the next booster
                Node = Scene.Tracker.GetNearestEntity<RailBooster>(
                    player.Collider.Center + player.ExactPosition
                    );
            }
        }
        #endregion

        #endregion
    }
}
