using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using OnHook = On.Celeste;
using C = Celeste;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Verillia.Utils
{
    [Tracked(true)]
    public class EventFirer : Entity
    {
        public enum Condition
        {
            #region Heart (HOOKS UNFINISHED)
            CategoryHeart = 0,
            //When the player breaks a heart.
            OnHeartCollect = 0,
            //When the text is shown.
            OnHeartText,
            //When the collection sequence ends
            OnHeartCollectEnd,
            #endregion

            #region Player (HOOKS MIDWAY FINISHED)
            CategoryPlayer = 100,
            //When the player dashes
            OnDash = 100,
            //When the player stops dashing
            OnDashEnd,
            //When the player supers
            OnSuper = 105,
            //When the player hypers
            OnHyper,
            //When the player wallbounces
            OnWallbounce,
            //When the player dreamjumps
            OnDreamJump,
            //When the player dies
            OnDie = 110,
            //When the player explodes
            OnDieEnd,
            //When the player respawns
            OnRespawn,
            //When the player jumps
            OnJump = 120,
            //When the player walljumps
            OnWalljump,
            //When the player climbjumps
            OnClimbjump,
            //When the player grabs a wall
            OnCling = 130,
            //When the player lets go of a wall
            OnRelease,
            //When the player grabs an object (also runs at the end of a transition)
            OnCarry = 140,
            //When the player throws an object (doesn't include neutral drops)
            OnThrow,
            //When the player drops an object
            OnDrop,
            //When the player ducks (also runs at the end of a transition)
            OnDuck = 150,
            //When the player unducks,
            OnUnduck,
            //When the player looks in a direction (also runs at the end of a transition)
            OnFacingRight = 160,
            OnFacingLeft,
            //When the player turns around
            OnTurn,
            //When the player lands (also runs at the end of a transition)
            OnLand = 180,
            //When the player enters an airborn state (also runs at the end of a transition)
            OnAirborn,
            //When the player is launched (baddy boost)
            OnLaunch = 185,
            //When the player stops being launched
            OnLaunchEnd,
            #endregion

            #region Collectibles (HOOKS UNFINISHED)
            CategoryCollectibles = 200,
            //When the closest key is collected.
            OnKeyCollected = 200,
            //When the closest berry starts following the player
            OnBerryGot = 210,
            //When the closest berry has been collected
            OnBerryCollected,
            //When any GOLDEN berry has been collected in the room
            OnGoldBerryCollected,
            //When the player happens to have brought a berry into a room (by transition)
            OnBerryBrought = 220,
            //When any GOLDEN berry is brought into the room (either by load or transition)
            OnGoldBerryBrought,
            #endregion

            #region Locked Objected (HOOKS UNFINISHED)
            CategoryDoors = 300,
            //When the closest lockblock has been unlocked and unblocked
            OnLockBlockOpened = 300,
            //When the closest open temple gate shuts behind Madeline
            OnTempleGateShut = 310,
            //When the closest closed temple gate opens.
            OnTempleGateOpened,
            #endregion

            #region Cassette Beats (HOOKS UNFINISHED)
            CategoryRhythm = 400,
            //When a Casette beat happens
            OnCassetteBeat = 400,
            //Each of these are for the individual colors.
            OnCassetteBeatPink,
            OnCassetteBeatBlue,
            OnCassetteBeatYellow,
            OnCassettebeatGreen,
            //When a cassette beat is about to happen in a "cassette tick"
            OnCassettePreBeat = 410,
            //Again, individual colors.
            OnCassettePreBeatPink,
            OnCassettePreBeatBlue,
            OnCassettePreBeatYellow,
            OnCassettePreBeatGreen,
            #endregion

            #region Rooms (HOOKS UNFINISHED)
            CategoryTransition = 500,
            //When a room is entered (post-transition).
            OnRoomEntered = 500,
            //When a room is exited (pre-transition).
            OnRoomExited,
            //When the player loads into this room (no transition)
            OnRoomEnteredFromLoad = 510,
            //When the player enters a room from file select.
            OnRoomEnteredFromFile,
            //When the player enters a room from the overworld
            OnRoomEnteredFromOverworld,
            #endregion

            #region Frames (HOOKS UNFINISHED)
            CategoryFrames = 600,
            //Per frame... yes.
            OnFrame = 600,
            //When freeze frames end
            OnFreezeEnd,
            //When the player resumes (from pause)
            OnResume,
            #endregion

            #region Core Mode (HOOKS UNFINISHED)
            CategoryCoreMode = 700,
            //When the core mode becomes the following. This also runs after a transition.
            OnCoreNeutral = 700,
            OnCoreHot,
            OnCoreCold,
            #endregion
        }
        internal static class Hooks
        {
            public static class Heart
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }
            public static class Player
            {
                public static void DashBegin(OnHook.Player.orig_DashBegin orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnDash);
                }
                public static void DashEnd(OnHook.Player.orig_DashEnd orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnDashEnd);
                }

                public static void Jump(OnHook.Player.orig_Jump orig, C.Player self, bool a, bool b)
                {
                    orig(self, a, b);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnJump);
                    if (self.dreamJump)
                        self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnDreamJump);
                }
                public static void ClimbJump(OnHook.Player.orig_ClimbJump orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnJump);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnClimbjump);
                }
                public static void WallJump(OnHook.Player.orig_WallJump orig, C.Player self, int a)
                {
                    orig(self, a);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnJump);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnWalljump);
                }

                public static void SuperWallJump(OnHook.Player.orig_SuperWallJump orig, C.Player self, int a)
                {
                    orig(self, a);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnJump);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnWallbounce);
                }
                public static void SuperJump(OnHook.Player.orig_SuperJump orig, C.Player self)
                {
                    bool hyper = self.Ducking;
                    orig(self);
                    if (hyper)
                        self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnHyper);
                    else
                        self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnSuper);
                }

                public static void ClimbBegin(OnHook.Player.orig_ClimbBegin orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnCling);
                }
                public static void ClimbEnd(OnHook.Player.orig_ClimbEnd orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnRelease);
                }

                public static bool Pickup(OnHook.Player.orig_Pickup orig, C.Player self, Holdable a)
                {
                    bool ret = orig(self, a);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnCarry);
                    return ret;
                }
                public static void Throw(OnHook.Player.orig_Throw orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnThrow);
                }
                public static void Drop(OnHook.Player.orig_Drop orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnDrop);
                }

                //Launching (Badeline Boost)
                public static void SummitLaunchBegin(OnHook.Player.orig_SummitLaunchBegin orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnLaunch);
                }
                public static void StopSummitLaunch(OnHook.Player.orig_StopSummitLaunch orig, C.Player self)
                {
                    orig(self);
                    self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnLaunchEnd);
                }

                //Death (Link to playerdeadbody for OnDieEnd)
                //Respawn
                public static PlayerDeadBody Die(OnHook.Player.orig_Die orig, C.Player self, Vector2 direction, bool always, bool stats)
                {
                    Scene s = self.Scene;
                    //check for if the player actually dies.
                    PlayerDeadBody e = orig(self, direction, always, stats);
                    if (e is not null)
                        s.FireCustomVerUtilsEventsFromCondition(Condition.OnDie);
                    return e;

                }
                public static void DieEnd(OnHook.DeathEffect.orig_Update orig, C.DeathEffect self)
                {
                    if (self.Percent == 0f)
                        self.Scene.FireCustomVerUtilsEventsFromCondition(Condition.OnDieEnd);
                    orig(self);
                }

                // Turning and landing are at PlayerExt

                public static void INIT()
                {
                    OnHook.Player.DashBegin += DashBegin;
                    OnHook.Player.DashEnd += DashEnd;

                    OnHook.Player.Jump += Jump;
                    OnHook.Player.ClimbJump += ClimbJump;
                    OnHook.Player.WallJump += WallJump;

                    OnHook.Player.SuperWallJump += SuperWallJump;
                    OnHook.Player.SuperJump += SuperJump;

                    OnHook.Player.ClimbBegin += ClimbBegin;
                    OnHook.Player.ClimbEnd += ClimbEnd;

                    OnHook.Player.Pickup += Pickup;
                    OnHook.Player.Throw += Throw;
                    OnHook.Player.Drop += Drop;

                    OnHook.Player.SummitLaunchBegin += SummitLaunchBegin;
                    OnHook.Player.StopSummitLaunch += StopSummitLaunch;

                    OnHook.Player.Die += Die;
                    OnHook.DeathEffect.Update += DieEnd;
                }

                public static void DEINIT()
                {
                    OnHook.Player.DashBegin -= DashBegin;
                    OnHook.Player.DashEnd -= DashEnd;

                    OnHook.Player.Jump -= Jump;
                    OnHook.Player.ClimbJump -= ClimbJump;
                    OnHook.Player.WallJump -= WallJump;

                    OnHook.Player.SuperWallJump -= SuperWallJump;
                    OnHook.Player.SuperJump -= SuperJump;

                    OnHook.Player.ClimbBegin -= ClimbBegin;
                    OnHook.Player.ClimbEnd -= ClimbEnd;

                    OnHook.Player.Pickup -= Pickup;
                    OnHook.Player.Throw -= Throw;
                    OnHook.Player.Drop -= Drop;

                    OnHook.Player.SummitLaunchBegin += SummitLaunchBegin;
                    OnHook.Player.StopSummitLaunch += StopSummitLaunch;

                    OnHook.Player.Die += Die;
                    OnHook.DeathEffect.Update += DieEnd;
                }
            }
            public static class Cassette
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }
            public static class KeyAndBerry
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }
            public static class LockBlockAndTempleGate
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }
            public static class Room
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }
            public static class Frame
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }
            public static class Core
            {
                public static void INIT()
                {

                }
                public static void DEINIT()
                {

                }
            }

            internal static void Init()
            {
                Heart.INIT();
                Player.INIT();
                KeyAndBerry.INIT();
                LockBlockAndTempleGate.INIT();
                Room.INIT();
                Frame.INIT();
                Core.INIT();
                Cassette.INIT();
            }
            internal static void DeInit()
            {
                Heart.DEINIT();
                Player.DEINIT();
                KeyAndBerry.DEINIT();
                LockBlockAndTempleGate.DEINIT();
                Room.DEINIT();
                Frame.DEINIT();
                Core.DEINIT();
                Cassette.DEINIT();
            }
        }
        public readonly Condition FireCondition = Condition.OnFrame;
        public readonly string EventName =
            "Hello! Verillia here! " +
            "If you see this message being debug logged then that means one of two things: " +
            "One, you or I forgot to override this. " +
            "Two, you really like to joke around and made the event name... this thing.";
        public readonly bool RunThroughRooms = true;
        public float Delay = 0f;
        private bool FromRoom = true;
        private Entity check;
        private bool ConditionFulfilled = false;
        private TransitionListener transition;
        public EventFirer(bool active, Condition fireCondition, string eventName, float delay = 0f, bool runThroughRooms = true)
        {
            Depth = VerUtils.Depths.FirstUpdate;
            Active = active;
            Visible = false;

            FireCondition = fireCondition;
            EventName = eventName;
            RunThroughRooms = runThroughRooms;
            Delay = delay;

            Add(transition = new TransitionListener());
            transition.OnOutBegin = () => { FromRoom = false; };
            transition.OnInEnd = FireTransition;
        }
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            LinkToEntity(scene);
        }
        private void FireTransition()
        {
            Player Maddy = Scene.Tracker.GetEntity<Player>();
            if (Maddy is null && (FireCondition - Condition.CategoryPlayer < 100))
            {
                Logger.Log(LogLevel.Error, "VerUtils/EventFirer",
                    $"Where did Maddy go?");
                return;
            }
            switch (FireCondition)
            {
                case Condition.OnFacingRight:
                    if (Maddy.Facing == Facings.Right)
                        FireEvent();
                    break;
                case Condition.OnFacingLeft:
                    if (Maddy.Facing == Facings.Left)
                        FireEvent();
                    break;
                case Condition.OnCarry:
                    if (Maddy.Holding is not null)
                        FireEvent();
                    break;
                case Condition.OnDuck:
                    if (Maddy.Ducking)
                        FireEvent();
                    break;
                case Condition.OnLand:
                    if(Maddy.onGround)
                        FireEvent();
                    break;
                case Condition.OnAirborn:
                    if (!Maddy.onGround)
                        FireEvent();
                    break;
            }
        }
        public void FireEvent(bool immediately = false)
        {
            //Special Cases to determine whether or not to push through
            if (!JustFulfilled(ConsiderSpecialCases()))
                return;
            //If not from room, calls don't count.
            if (!FromRoom && FireCondition != Condition.OnRoomExited)
                return;
            if (immediately)
            {
                Scene.FireCustomVerUtilsEvent(EventName);
                return;
            }
            Alarm.Set(this, Delay, () => { Scene.FireCustomVerUtilsEvent(EventName); });
        }
        private bool ConsiderSpecialCases()
        {
            //if the condition it the room being exited, then say if it isn't from the room.
            if (FireCondition == Condition.OnRoomExited)
                return !FromRoom;
            //if no special case, then just say it is a pass.
            if (check is null)
            {
                ConditionFulfilled = false;
                return true;
            }
            //Pass through each of the conditions
            switch (FireCondition)
            {
                case Condition.OnKeyCollected:
                    Key key = check as Key;
                    return key.follower.HasLeader;

                case Condition.OnBerryGot:
                    Strawberry berry = check as Strawberry;
                    return berry.Follower.HasLeader;

                case Condition.OnBerryCollected:
                case Condition.OnGoldBerryCollected:
                    berry = check as Strawberry;
                    return berry.collected;

                case Condition.OnLockBlockOpened:
                    LockBlock lockblock = check as LockBlock;
                    return lockblock.UnlockingRegistered;

                case Condition.OnTempleGateShut:
                    TempleGate gate = check as TempleGate;
                    return !gate.open;

                case Condition.OnTempleGateOpened:
                    gate = check as TempleGate;
                    return gate.open;
            }
            ConditionFulfilled = false;
            return true;
        }
        private bool JustFulfilled(bool condition)
        {
            if (!condition || ConditionFulfilled)
                return false;
            ConditionFulfilled = true;
            return true;
        }
        private void LinkToEntity(Scene scene)
        {
            switch (FireCondition)
            {
                case Condition.OnKeyCollected:
                    check = scene.PseudoTrackNearestTo<Key>(Position);
                    break;
                case Condition.OnBerryGot:
                case Condition.OnBerryCollected:
                case Condition.OnGoldBerryCollected:
                    check = scene.PseudoTrackNearestTo<Strawberry>(Position);
                    break;

                case Condition.OnLockBlockOpened:
                    check = scene.PseudoTrackNearestTo<TempleGate>(Position);
                    break;

                case Condition.OnTempleGateShut:
                case Condition.OnTempleGateOpened:
                    check = scene.PseudoTrackNearestTo<TempleGate>(Position);
                    break;
            }
            if (check is not null)
            {
                ConditionFulfilled = ConsiderSpecialCases();
            }
        }
        public override void Update()
        {
            base.Update();
            Tag = Tag.WithTag(Tags.Global, (Components.GetAll<Alarm>().Count() == 0));
            ConditionFulfilled = ConsiderSpecialCases();
            if (FromRoom || (RunThroughRooms && Components.GetAll<Alarm>().Count() != 0))
                return;
            RemoveSelf();
        }
    }
}
