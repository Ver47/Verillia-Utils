using System;
using System.Collections;
using Celeste.Mod.Verillia.Utils.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;
using Monocle;
using Mono.Cecil.Cil;
using System.Reflection;

namespace Celeste.Mod.Verillia.Utils
{
    public class VerilliaUtilsModule : EverestModule
    {
        public const string ModName = "VerUtils";

        public static VerilliaUtilsModule Instance { get; private set; }

        public override Type SettingsType => typeof(VerilliaUtilsSettings);
        public static VerilliaUtilsSettings Settings => (VerilliaUtilsSettings)Instance._Settings;

        public override Type SessionType => typeof(VerilliaUtilsSession);
        public static VerilliaUtilsSession Session => (VerilliaUtilsSession)Instance._Session;

        public VerilliaUtilsModule()
        {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(ModName, LogLevel.Debug);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(ModName, LogLevel.Info);
#endif
        }

        public override void Load()
        {
            //Template stuff
            On.Celeste.LevelLoader.ctor += LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor += OverworldLoader_ctor;

            //Player creation
            On.Celeste.Player.ctor += Player_ctor;
            Everest.Events.Player.OnRegisterStates += Player_addStates;

            //Player update and render
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.Render += Player_Render;

            //Player methods.
            On.Celeste.Player.Die += Player_die;
            On.Celeste.PlayerCollider.Check += PlayerCollider_Check;
            //PlayerLiftBoostHook = new ILHook(typeof(Player).GetProperty("LiftBoost", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true), Player_LiftBoost_get);

            //Actor methods
            ActorLiftBoostHook = new Hook(typeof(Actor).GetProperty("LiftSpeed",
                BindingFlags.Instance | BindingFlags.Public).GetGetMethod(), getLiftBoost);
            On.Celeste.Actor.MoveHExact += MoveHExact;
            On.Celeste.Actor.MoveVExact += MoveVExact;
            On.Celeste.Actor.ctor += Actor_ctor;
            On.Celeste.Actor.Update += Actor_Update;

            //Custom Event Conditions
            EventFirer.Hooks.Init();
        }

        public override void Unload()
        {
            //Template stuff
            On.Celeste.LevelLoader.ctor -= LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor -= OverworldLoader_ctor;

            //Player creation
            On.Celeste.Player.ctor -= Player_ctor;
            Everest.Events.Player.OnRegisterStates -= Player_addStates;

            //Player update and render
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.Render -= Player_Render;

            //Player methods.
            On.Celeste.Player.Die -= Player_die;
            On.Celeste.PlayerCollider.Check -= PlayerCollider_Check;
            //PlayerLiftBoostHook.Dispose();

            //Actor methods
            ActorLiftBoostHook.Dispose();
            On.Celeste.Actor.MoveHExact -= MoveHExact;
            On.Celeste.Actor.MoveVExact -= MoveVExact;
            On.Celeste.Actor.ctor -= Actor_ctor;
            On.Celeste.Actor.Update -= Actor_Update;

            //Custom Event Conditions
            EventFirer.Hooks.DeInit();
        }

        #region Level Hooks
        public void LoadBeforeLevel()
        {


            // TODO: apply any hooks that should only be active while a level is loaded
        }

        public void UnloadAfterLevel()
        {


            // TODO: unapply any hooks applied in LoadBeforeLevel()
        }

        //What are these? I dunno, but they do things!
        private void OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow)
        {
            orig(self, startmode, snow);
            if (startmode != (Overworld.StartMode)(-1))
            {
                UnloadAfterLevel();
            }
        }
        private void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startposition)
        {
            orig(self, session, startposition);
            LoadBeforeLevel();
        }
        #endregion

        #region Player Hooks
        private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 pos, PlayerSpriteMode spriteMode)
        {
            orig(self, pos, spriteMode);
            //Just to ensure its existence.
            self.GetVerUtilsExt();
        }

        private void Player_addStates(Player player)
        {
            player.GetVerUtilsExt().InitializeStates();
        }

        private PlayerDeadBody Player_die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true)
        {
            if (!evenIfInvincible && self.GetVerUtilsExt().Invincible)
            {
                return null;
            }
            var deadbody = orig(self, direction, evenIfInvincible, registerDeathInStats);
            if (deadbody is not null)
                self.GetVerUtilsExt().playerRailBooster?.Burst();
            return deadbody;
        }

        //Player update and render, not using the getext in case somebody wants to remove the component
        private void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            var ext = self.Components.Get<VerilliaUtilsPlayerExt>();
            if (ext is null)
            {
                orig(self);
                return;
            }
            ext.PreUpdate();
            orig(self);
            ext.PostUpdate();
        }

        private void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            var ext = self.Components.Get<VerilliaUtilsPlayerExt>();
            if (ext is null)
            {
                orig(self);
                return;
            }
            ext.RenderBelow();
            orig(self);
            ext.RenderAbove();
        }

        private bool PlayerCollider_Check(On.Celeste.PlayerCollider.orig_Check orig, PlayerCollider self, Player player)
        {
            var ext = player.Components.Get<VerilliaUtilsPlayerExt>();
            ext?.SetSpeed(false);
            var ret = orig(self, player);
            ext?.SetSpeed(true);
            return ret;
        }

        ILHook PlayerLiftBoostHook;
        private void Player_LiftBoost_get(ILContext il)
        {
            //Thanks for Viv for doing a fuck ton of this

            ILCursor cursor = new(il);
            ILCursor point1 = cursor.Clone();
            if (!point1.TryGotoNext(MoveType.After, i => i.MatchLdcR4(250))) {
                Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                "Someone decided the remove the vertical liftspeed cap in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                return;
            }
            ILCursor point2 = point1.Clone();
            if (!point2.TryGotoNext(MoveType.After, i => i.MatchLdcR4(-130))) {
                Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                "Someone decided the remove the vertical liftspeed cap in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                return;
            }

            VariableDefinition v_LiftShift = new VariableDefinition(il.Import(typeof(Vector2))); // creates a new local variable in get_LiftBoost
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(VerilliaUtilsModule).GetMethod("get_LiftShift", (BindingFlags)40));
            cursor.Emit(OpCodes.Stloc, v_LiftShift); // Stores a value to local V_LiftShift

            //Shift the horizontal caps
            point1.Emit(OpCodes.Ldloc, v_LiftShift);
            point1.Emit(OpCodes.Ldfld, typeof(Vector2).GetField("X"));
            point1.Emit(OpCodes.Add);

            //Shift the vertical caps
            point2.Emit(OpCodes.Ldloc, v_LiftShift);
            point2.Emit(OpCodes.Ldfld, typeof(Vector2).GetField("Y"));
            point2.Emit(OpCodes.Add);
        }

        private static Vector2 get_LiftShift(Entity e)
        {
            var ret = Vector2.Zero;
            foreach(SpeedBonus c in e.Components.GetAll<SpeedBonus>())
            {
                ret = c.GetLiftSpeedCapShift(ret);
            }
            return ret;
        }
        #endregion

        #region Actor Hooks
        Hook ActorLiftBoostHook;
        private delegate Vector2 orig_get_LiftSpeed(Actor self);
        private Vector2 getLiftBoost(orig_get_LiftSpeed orig, Actor self)
        {
            Vector2 speed = orig(self);
            foreach (SpeedBonus liftspeed in self.Components.GetAll<SpeedBonus>())
                speed = liftspeed.GetLiftSpeed(speed);
            return speed;
        }

        private bool MoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide = null, Solid pusher = null)
        {
            int goalpos = (int)self.Position.X + moveH;
            bool ret = orig(self, moveH, onCollide, pusher);
            if (ret || pusher is not null)
                return ret;
            var over = self.GetOverpass();
            if (over.Active)
                over.H += goalpos - (int)self.Position.X;
            return ret;
        }

        private bool MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide = null, Solid pusher = null)
        {
            int goalpos = (int)self.Position.Y + moveV;
            bool ret = orig(self, moveV, onCollide, pusher);
            if (ret || pusher is not null)
                return ret;
            var over = self.GetOverpass();
            if (over.Active)
                over.V += goalpos - (int)self.Position.Y;
            return ret;
        }

        private void Actor_ctor(On.Celeste.Actor.orig_ctor orig, Actor self, Vector2 pos)
        {
            orig(self, pos);
            self.GetOverpass();
        }

        private void Actor_Update(On.Celeste.Actor.orig_Update orig, Actor self)
        {
            foreach (SpeedBonus sped in self.Components.GetAll<SpeedBonus>())
            {
                //Run moving code
                sped.DoTheMovie();
            }
            self.GetOverpass().Reset();
            orig(self);
        }
        #endregion
    }
}