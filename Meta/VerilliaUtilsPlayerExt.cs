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
using System.Net;

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

        //Speed stuff
        internal Vector2 Velocity; // speed appeared outside
        internal Vector2 internalSpeed; // speed moved by player itself
        internal bool manualMovement;

        //Event Firing
        private bool WasOnGround;
        private Facings WasFacing;

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
                "Removing this may pose some risks to functionality. Be wary.");
            base.Removed(entity);
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            WasOnGround = player.onGround;
            WasFacing = 0;
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
            SetSpeed(true);
        }

        public void PostUpdate()
        {
            //Event Calls
            if (WasOnGround != player.onGround)
                if (WasOnGround)
                    Scene.FireCustomVerUtilsEventsFromCondition(EventFirer.Condition.OnAirborn);
                else
                    Scene.FireCustomVerUtilsEventsFromCondition(EventFirer.Condition.OnLand);
            WasOnGround = player.onGround;
            if (WasFacing != player.Facing)
            {
                switch (player.Facing)
                {
                    case Facings.Left:
                        Scene.FireCustomVerUtilsEventsFromCondition(EventFirer.Condition.OnFacingLeft);
                        break;
                    case Facings.Right:
                        Scene.FireCustomVerUtilsEventsFromCondition(EventFirer.Condition.OnFacingRight);
                        break;
                }
                if (WasFacing != 0)
                    Scene.FireCustomVerUtilsEventsFromCondition(EventFirer.Condition.OnTurn);
            }
            WasFacing = player.Facing;

            SetSpeed(false);
        }

        internal void SetSpeed(bool Internal)
        {
            if (!manualMovement)
                return;
            if (Internal)
            {
                Velocity = manualMovement ? player.Speed : Velocity;
                player.Speed = manualMovement ? internalSpeed : player.Speed;
            }
            else
            {
                internalSpeed = player.Speed;
                player.Speed = manualMovement ? Velocity : player.Speed;
            }
        }

        #endregion
        #endregion

        #region HOOKS
        #endregion

        #region STATES

        internal void InitializeStates()
        {
            Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
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
        public const float RailBoosterTravelSpeed = 180f;
        public const float RailBoosterSpitSpeed = 200f;

        public const float RailBoosterSpitHBoost = 125f; // add to speed
        public const float RailBoosterHBoostReq = 170f; //absolute must be higher than this

        public const float RailBoosterSpitDBoostH = 90f; // adds to horizontal
        public const float RailBoosterSpitDBoostV = 50f; // adds to vertical

        public const float RailBoosterSpitVBoost = -45f; // minimum jump speed
        public const float RailBoosterVBoostReq = 45f; // must be less
        public const float RailBoosterVBoostTimeMin = 0.15f; // Autojump time
        public const float RailBoosterVBoostTimeMax = 0.2f; // Varjump time

        public const float RailBoosterSpitVBoostAlt = 80f; //Add to vertical
        public const float RailBoosterVBoostReqAlt = 90f; // Horizontal must be less

        internal RailBooster LastRailBooster;
        internal RailBooster NextRailBooster;
        internal RailBooster.PlayerRailBooster playerRailBooster;

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
            Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                "Starting railboost");
            player.TreatNaive = true;
            // Start of by getting the booster you are going through
            LastRailBooster = Scene.Tracker.GetNearestEntity<RailBooster>(
                player.Collider.Center + player.ExactPosition
                );
            int RailIndex = -1;
            //Defaults to player speed, helps on instant choice and spit tech
            Velocity = player.Speed;
            Facings Heading;
            if (Velocity.X == 0)
                Heading = player.Facing;
            else
                Heading = Velocity.X > 0 ? Facings.Right : Facings.Left;
            //the player having speed in this state is going to be annoying.
            player.Speed = Vector2.Zero;
            manualMovement = true;
            playerRailBooster.sprite.Scale.X = (int)player.Facing;

            #region Sucking
            float Timer = RailBooster.CenterSuckTime;
            Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                $"Going to entry node at {LastRailBooster.Position}");
            Vector2 playerEnterPosition = player.ExactPosition;
            Vector2 movegoal;
            while (Timer > 0 &&
                (player.ExactPosition != LastRailBooster.Center-player.Collider.Center))
            {
                if (Input.DashPressed)
                    break;
                yield return null;
                movegoal = Calc.Approach(
                    player.ExactPosition,
                    LastRailBooster.Center-player.Collider.Center,
                    RailBooster.CenterSuckSpeed * Engine.DeltaTime
                    );
                player.NaiveMove(movegoal - player.ExactPosition);
                Timer -= Engine.DeltaTime;
            }
            bool justEntered = true;
            //Lock the player unto the railbooster
            player.NaiveMove(
                (LastRailBooster.Center - player.Collider.Center)
                - player.ExactPosition
                );
            #endregion

            player.Visible = false;

            //Have it on endless loop until broken
            while (true)
            {
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"Railbooster node has {LastRailBooster.Rails.Count} options.");
                player.RefillDash();
                Invincible = false;

                #region Exit
                // Immediately end the action on exit
                // please ensure that there is a way to return to StNormal
                if (LastRailBooster.Exit(justEntered))
                {
                    Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                        $"Exiting railboost with a Velocity of {Velocity}");
                    player.Facing = Heading;
                    Velocity = Velocity.SafeNormalize(RailBoosterSpitSpeed);

                    player.Visible = true;
                    playerRailBooster.Burst();

                    bool VBoostJump = false;
                    if (Math.Abs(Velocity.X) >= RailBoosterHBoostReq)
                    {
                        Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtenion",
                            $"Reached HBoost threshold with {Velocity.X}");
                        Velocity.X += Math.Sign(Velocity.X) * RailBoosterSpitHBoost;
                        VBoostJump = true;
                    }
                    else if (Math.Abs(Velocity.X) <= RailBoosterVBoostReqAlt)
                    {
                        Velocity.Y += Math.Sign(Velocity.Y) * RailBoosterSpitVBoostAlt;
                        player.varJumpSpeed = Velocity.Y;
                        player.AutoJumpTimer = RailBoosterVBoostTimeMin;
                        player.varJumpTimer = RailBoosterVBoostTimeMax;
                        player.AutoJump = true;
                    }
                    else
                    {
                        Velocity.X += Math.Sign(Velocity.X) * RailBoosterSpitDBoostH;
                        Velocity.Y += Math.Sign(Velocity.Y) * RailBoosterSpitDBoostV;
                        player.AutoJumpTimer = RailBoosterVBoostTimeMin;
                        player.varJumpTimer = RailBoosterVBoostTimeMax;
                        player.AutoJump = true;
                    }

                    if (Velocity.Y <= RailBoosterVBoostReq)
                    {
                        Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtenion",
                            $"Reached VBoost threshold with {Velocity.Y}");
                        Velocity.Y = Math.Min(Velocity.Y, RailBoosterSpitVBoost);
                        player.varJumpSpeed = Velocity.Y;
                        if (VBoostJump)
                        {
                            player.AutoJumpTimer = RailBoosterVBoostTimeMin;
                            player.varJumpTimer = RailBoosterVBoostTimeMax;
                            player.AutoJump = true;
                        }
                    }

                    player.Speed = Velocity;
                    player.StateMachine.State = Player.StNormal;
                    player.launched = true;
                    Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                        $"Launched at speed {player.Speed}");
                    // Set the timer as so.
                    LastRailBooster.ResetTimer();
                    // Just a bit paranoid
                    NextRailBooster = null;

                    //prepare for whatever
                    player.TreatNaive = false;
                    manualMovement = false;
                    //Shock it
                    Celeste.Freeze(0.05f);
                    yield break;
                }
                #endregion

                playerRailBooster.Idle();
                justEntered = false;
                Celeste.Freeze(0.05f);
                yield return null;

                #region RailChoice
                // Returning the given integer means that the player would have to choose manually
                // maybe replace that with a pass through check or something?
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    "Choosing rail");
                int index = LastRailBooster.getDefault(RailIndex);
                if (index == RailIndex)
                {

                    //Player gets limited decision time before they continue
                    //(Unless the booster is instant)
                    Timer = LastRailBooster.getTimeLimit();
                    while (Timer > 0)
                    {
                        if (Input.DashPressed)
                            break;
                        yield return null;
                        Timer -= Engine.DeltaTime;
                    }

                    //ToDo:
                    //Get AimVector, defaulting to the velocity if not chosen
                    Vector2 aim = Input.Aim.Value == Vector2.Zero? Velocity.SafeNormalize() : Input.GetAimVector();
                    RailIndex = LastRailBooster.getClosestToDirection(aim, RailIndex);
                }
                else
                {
                    // Go to the default if autodecided
                    RailIndex = index;
                }
                #endregion

                #region Rail Movement
                //player railbooster setup.
                playerRailBooster.Phase = RailBooster.PlayerRailBooster.Phases.Move;
                // Set the timer as so.
                LastRailBooster.ResetTimer();
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"Travelling through rail #{RailIndex}");
                RailRope Rail = LastRailBooster.Rails[RailIndex];
                if (Rail.Depth == VerUtils.Depths.RailBooster_Rail_BG)
                {
                    playerRailBooster.BG = true;
                }
                if (Rail.endA == LastRailBooster)
                {
                    NextRailBooster = Rail.endB;
                    RailIndex = Rail.indexB;
                }
                else
                {
                    NextRailBooster = Rail.endA;
                    RailIndex = Rail.indexA;
                }
                // Whether or not the player is invincible is based on the rail.
                Invincible = Rail.InvincibleOnTravel;
                // bunch of movement logic for rails
                SimpleCurve RailBoosterPath = Rail.getPathFrom(LastRailBooster.Position);
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"Rail Details: {{Begin: {RailBoosterPath.Begin}, Control: {RailBoosterPath.Control}, End: {RailBoosterPath.End}}}");
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"Rail is of length: {RailBoosterPath.GetLengthParametric(Rail.PointCount)}");
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"Points to travel through: {Rail.PointCount}");

                int e = Math.Sign(RailBoosterPath.End.X - RailBoosterPath.Begin.X);
                player.Facing = e != 0 ? (Facings)e : player.Facing;
                playerRailBooster.sprite.Scale.X = (int)player.Facing;

                //Distance travelled gets decided (setup)
                float dist = RailBoosterTravelSpeed * Engine.DeltaTime;
                Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                $"Distance for this frame: {dist}");
                // Move through the rail
                for (int pointindex = 0; pointindex <= Rail.PointCount; pointindex++)
                {
                    //Point for reference
                    Vector2 goal = RailBoosterPath.GetPoint((float)pointindex/Rail.PointCount) - player.Collider.Center;
                    Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                        $"Booster travel percent: {(float)pointindex/Rail.PointCount}");

                    Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                        $"Current node to travel to: {goal}");
                    Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                        $"Player position: {player.ExactPosition}");

                    Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                        $"Travelling through rail segment of length: {(player.ExactPosition - goal).Length()}");
                    while (player.ExactPosition != goal)
                    {
                        //Player Position gets set
                        movegoal = Calc.Approach(player.ExactPosition, goal, dist);
                        var seglen = (player.ExactPosition - goal).Length();
                        Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                            $"Travelling through rail segment of remaining length: {seglen}");
                        dist -= seglen;
                        player.NaiveMove(movegoal - player.ExactPosition);
                        Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                            $"Travelled, remaining distance: {Math.Max(dist, 0)}");
                        //goto next frame if needed distance is travelled
                        if (dist <= 0)
                        {
                            //to prevent spike issues...
                            Velocity = (goal - player.ExactPosition).SafeNormalize(RailBoosterTravelSpeed);
                            Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                                $"Current velocity: {Velocity}");
                            yield return null;
                            //next distance gets decided
                            dist = RailBoosterTravelSpeed * Engine.DeltaTime;
                            Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                            $"Distance for this frame: {dist}");
                        }
                        else
                        {
                            Logger.Log(LogLevel.Verbose, "VerUtils/PlayerExtension",
                                $"Remaining distance to be travelled: {dist}");
                        }
                    }
                }
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"RailTravel ended at {player.Position + player.Collider.Center}. Correcting...");
                //Put the player at the end of the rail.
                movegoal = RailBoosterPath.End - player.Collider.Center;
                player.NaiveMove(movegoal - player.ExactPosition);
                #endregion

                // set next heading.
                if (Velocity.X != 0)
                    Heading = Velocity.X > 0 ? Facings.Right : Facings.Left;
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"Ended rail at {player.Position + player.Collider.Center}");
                //Get the next booster
                LastRailBooster = (NextRailBooster.Scene == Scene)?
                    NextRailBooster :
                    Scene.Tracker.GetNearestEntity<RailBooster>(player.ExactPosition);
                Logger.Log(LogLevel.Debug, "VerUtils/PlayerExtension",
                    $"New node is at {LastRailBooster.Position}");
                playerRailBooster.BG = false;
            }
        }
        #endregion

        #endregion
    }
}
