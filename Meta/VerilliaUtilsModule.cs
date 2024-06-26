﻿using System;
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

        private HookPack hooks = new();

        public VerilliaUtilsModule()
        {
            Instance = this;
            hooks.LogName = ModName;
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
            Type t;

            //Template stuff
            t = typeof(On.Celeste.LevelLoader);
            hooks.Add(t, nameof(On.Celeste.LevelLoader.ctor), LevelLoader_ctor);

            t = typeof(On.Celeste.OverworldLoader);
            hooks.Add(t, nameof(On.Celeste.OverworldLoader.ctor), OverworldLoader_ctor);

            //Player
            t = typeof(On.Celeste.Player);
            hooks.Add(t, nameof(On.Celeste.Player.ctor), Player_ctor);
            hooks.Add(t, nameof(On.Celeste.Player.Update), Player_Update);
            hooks.Add(t, nameof(On.Celeste.Player.Render), Player_Render);
            hooks.Add(t, nameof(On.Celeste.Player.Die), Player_die);

            t = typeof(Everest.Events.Player);
            hooks.Add(t, nameof(Everest.Events.Player.OnRegisterStates), Player_addStates);

            t = typeof(On.Celeste.PlayerCollider);
            hooks.Add(t, nameof(On.Celeste.PlayerCollider.Check), PlayerCollider_Check);

            hooks.Add(new ILHook(typeof(Player).GetProperty(
                "LiftBoost",
                BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true),
                Player_LiftBoost_get));

            //Actor
            hooks.Add(new Hook(typeof(Actor).GetProperty(
                "LiftSpeed",
                BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                getLiftBoost));

            t = typeof(On.Celeste.Actor);
            hooks.Add(t, nameof(On.Celeste.Actor.ctor), Actor_ctor);

            //Entitylist methods
            t = typeof(IL.Monocle.EntityList);
            hooks.Add(t, nameof(IL.Monocle.EntityList.Update), EntityList_Update);
            hooks.Add(t, nameof(IL.Monocle.EntityList.Render), EntityList_Render);
            hooks.Add(t, nameof(IL.Monocle.EntityList.RenderOnly), EntityList_Render);
            hooks.Add(t, nameof(IL.Monocle.EntityList.RenderOnlyFullMatch), EntityList_Render);
            hooks.Add(t, nameof(IL.Monocle.EntityList.RenderExcept), EntityList_Render);

            //Custom Event Conditions
            EventFirer.Hooks.Init();
        }

        public override void Unload()
        {
            hooks.Dispose();

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

        private void Player_LiftBoost_get(ILContext il)
        {
            //Thanks to Viv for laying the foundation

            ILCursor cursor = new(il);

            //Vertical
            ILCursor point1a = cursor.Clone();
            ILCursor point1b;
            if (point1a.TryGotoNext(MoveType.Before, i => i.MatchLdcR4(250))
                && point1a.TryGotoPrev(MoveType.After, i => i.MatchLdfld(typeof(Vector2).GetField(nameof(Vector2.X)))))
            {
                point1b = point1a.Clone();
                if (!point1b.TryGotoNext(MoveType.Before,
                    i => i.MatchStfld(typeof(Vector2).GetField(nameof(Vector2.X)))))
                {
                    Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                    "Someone decided to mess with the horizontal liftspeed capping in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                    return;
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                    "Someone decided to remove the horizontal liftspeed cap in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                return;
            }

            //Upper Vertical
            ILCursor point2a = point1a.Clone();
            ILCursor point2b;
            if (point2a.TryGotoNext(MoveType.After, i => i.MatchLdcR4(-130)))
            {
                point2b = point2a.Clone();
                if (!point2b.TryGotoNext(MoveType.Before,
                    i => i.MatchStfld(typeof(Vector2).GetField(nameof(Vector2.Y)))))
                {
                    Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                    "Someone decided to mess with the vertical liftspeed capping in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                    return;
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                "Someone decided to remove the vertical liftspeed cap in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                return;
            }

            //Lower Vertical
            ILCursor point3a = point1a.Clone();
            ILCursor point3b;
            if (point3a.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0)))
            {
                point3b = point3a.Clone();
                if (!point3b.TryGotoNext(MoveType.Before,
                    i => i.MatchStfld(typeof(Vector2).GetField(nameof(Vector2.Y)))))
                {
                    Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                    "Someone decided to mess with the vertical liftspeed capping in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                    return;
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "VerUtils/SpeedBonus",
                "Someone decided to remove the vertical liftspeed cap in Celeste.Player::get_LiftSpeed(). This is going to result in some bugs.");
                return;
            }

            VariableDefinition v_LiftShift = new VariableDefinition(il.Import(typeof(Vector2))); // creates a new local variable in get_LiftBoost
            il.Body.Variables.Add(v_LiftShift);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(VerilliaUtilsModule).GetMethod("get_LiftShift",
                BindingFlags.Public | BindingFlags.Static));
            cursor.Emit(OpCodes.Stloc, v_LiftShift); // Stores a value to local V_LiftShift

            //Shift the horizontal caps
            point1a.EmitLdloc(v_LiftShift);
            point1a.EmitLdfld(typeof(Vector2).GetField(nameof(Vector2.X)));
            point1a.Emit(OpCodes.Sub);

            point1b.EmitLdloc(v_LiftShift);
            point1b.EmitLdfld(typeof(Vector2).GetField(nameof(Vector2.X)));
            point1b.Emit(OpCodes.Add);

            //Shift the upper vertical cap
            point2a.EmitLdloc(v_LiftShift);
            point2a.EmitLdfld(typeof(Vector2).GetField(nameof(Vector2.Y)));
            point2a.Emit(OpCodes.Add);

            point2b.EmitLdloc(v_LiftShift);
            point2b.EmitLdfld(typeof(Vector2).GetField(nameof(Vector2.Y)));
            point2b.Emit(OpCodes.Add);

            //Shift the lower vertical cap
            point3a.EmitLdloc(v_LiftShift);
            point3a.EmitLdfld(typeof(Vector2).GetField(nameof(Vector2.Y)));
            point3a.Emit(OpCodes.Add);

            point3b.EmitLdloc(v_LiftShift);
            point3b.EmitLdfld(typeof(Vector2).GetField(nameof(Vector2.Y)));
            point3b.Emit(OpCodes.Add);
        }

        public static Vector2 get_LiftShift(Actor e)
        {
            var ret = Vector2.Zero;
            var sped = e.LiftSpeed;
            foreach(SpeedBonus c in e.Components.GetAll<SpeedBonus>())
            {
                ret = c.GetLiftSpeedCapShift(ret, sped);
            }
            return ret;
        }
        #endregion

        #region Actor Hooks
        private delegate Vector2 orig_get_LiftSpeed(Actor self);
        private Vector2 getLiftBoost(orig_get_LiftSpeed orig, Actor self)
        {
            Vector2 speed = orig(self);
            foreach (SpeedBonus liftspeed in self.Components.GetAll<SpeedBonus>())
                speed = liftspeed.GetLiftSpeed(speed);
            return speed;
        }

        private void Actor_ctor(On.Celeste.Actor.orig_ctor orig, Actor self, Vector2 pos)
        {
            orig(self, pos);
            self.GetCounterMovement();
        }
        #endregion

        #region Entitylist Hooks

        private void EntityList_Update(ILContext il)
        {
            ApplyBeforeAfter<Entity>(il, nameof(Entity.Update), 1, VerUtils.PreUpdate, VerUtils.PostUpdate);
        }

        private void EntityList_Render(ILContext il)
        {
            ApplyBeforeAfter<Entity>(il, nameof(Entity.Render), 1, VerUtils.PreRender, VerUtils.PostRender);
        }

        private void ApplyBeforeAfter<T>(ILContext il, string functionName, int index, Action<T> Before, Action<T> After)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, i => i.MatchCallvirt<T>(functionName)))
            {
                cursor.EmitDelegate(Before);
                cursor.EmitLdloc(index);
                cursor.GotoNext(MoveType.After, i => true); // The function
                cursor.EmitLdloc(index);
                cursor.EmitDelegate(After);
            }
        }

        #endregion
    }
}