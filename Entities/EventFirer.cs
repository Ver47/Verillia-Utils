using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;

namespace Celeste.Mod.Verillia.Utils
{
    [Tracked(true)]
    public class EventFirer : Entity
    {
        public enum Condition
        {
            //When the player breaks a heart.
            OnHeartCollect = 0,
            //When the text is shown.
            OnHeartText,
            //When the collection sequence ends
            OnHeartCollectEnd,

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
            //When the player grabs an object
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
            //When the player enters a dummy state (includes badeline boost and booster transition)
            OnCutsceneDummy = 170,
            //When the player leaves a dummy state
            OnCutsceneDummyEnd,
            //When the player lands
            OnLand = 180,
            //When the player enters an airborn state (also runs at the end of a transition)
            OnAirborn,
            //When the player is launched (lift boost)
            OnLaunch = 185,
            //When the player stops being launched
            OnLaunchEnd,


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

            //When the closest lockblock has been unlocked and unblocked
            OnLockBlockOpened = 300,
            //When the closest open temple gate shuts behind Madeline
            OnTempleGateShut = 310,
            //When the closest closed temple gate opens.
            OnTempleGateOpened,

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

            //Per frame... yes.
            OnFrame = 600,
            //When freeze frames end
            OnFreezeEnd,
            //When the player resumes
            OnResume,

            //When the core mode becomes the following. This also runs after a transition.
            OnCoreNeutral = 700,
            OnCoreHot,
            OnCoreCold,
        }
        internal static void InitConditionHooks()
        {

        }
        internal static void DeInitConditionHooks()
        {

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
            FireCondition = fireCondition;
            EventName = eventName;
            RunThroughRooms = runThroughRooms;
            Delay = delay;
            Active = active;
            Add(transition = new TransitionListener());
            transition.OnOutBegin = () => { FromRoom = false; };
        }
        public override void Awake(Scene scene)
        {
            LinkToEntity(scene);
            base.Awake(scene);
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
                Scene.FireVerUtilsEvent(EventName);
                return;
            }
            Alarm.Set(this, Delay, () => { Scene.FireVerUtilsEvent(EventName); });
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
