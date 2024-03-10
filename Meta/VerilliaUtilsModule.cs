using System;
using System.Collections;
using Microsoft.Xna.Framework;
using MonoMod.ModInterop;

namespace Celeste.Mod.Verillia.Utils {
    public class VerilliaUtilsModule : EverestModule {
        public const string ModName = "VerUtils";

        public static VerilliaUtilsModule Instance { get; private set; }

        public override Type SettingsType => typeof(VerilliaUtilsSettings);
        public static VerilliaUtilsSettings Settings => (VerilliaUtilsSettings) Instance._Settings;

        public override Type SessionType => typeof(VerilliaUtilsSession);
        public static VerilliaUtilsSession Session => (VerilliaUtilsSession) Instance._Session;

        public VerilliaUtilsModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(ModName, LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(ModName, LogLevel.Info);
#endif
        }

        public override void Load() {
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
        }

        public void LoadBeforeLevel() {
            

            // TODO: apply any hooks that should only be active while a level is loaded
        }

        public void UnloadAfterLevel() {
            

            // TODO: unapply any hooks applied in LoadBeforeLevel()
        }

        //What are these? I dunno, but they do things!
        private void OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow) {
            orig(self, startmode, snow);
            if (startmode != (Overworld.StartMode) (-1)) {
                UnloadAfterLevel();
            }
        }
        private void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startposition) {
            orig(self, session, startposition);
            LoadBeforeLevel();
        }

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
            return orig(self, direction, evenIfInvincible, registerDeathInStats);
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
    }
}