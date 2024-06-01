using Celeste.Mod.Verillia.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VanillaDepths = Celeste.Depths;

namespace Celeste.Mod.Verillia.Utils
{
    public static class VerUtils
    {
        //Mod Meta
        public static string ModName => VerilliaUtilsModule.ModName;
        public static VerilliaUtilsModule Mod => VerilliaUtilsModule.Instance;
        public static VerilliaUtilsSettings Settings => VerilliaUtilsModule.Settings;
        public static VerilliaUtilsSession Session => VerilliaUtilsModule.Session;

        #region ExtensionMethods

        #region Player
        //For ease of getting the extension
        public static VerilliaUtilsPlayerExt GetVerUtilsExt(this Player player)
        {
            var playerExt = player.Components.Get<VerilliaUtilsPlayerExt>();
            if (playerExt != null)
                return playerExt;
            Logger.Log(LogLevel.Info, "VerUtils",
                "Adding Player Extension component to Player");
            player.Add(playerExt = new VerilliaUtilsPlayerExt());
            return playerExt;
        }
        #endregion

        #region Scene
        private static Queue<string> EventCallBuffer = new Queue<string>();
        private static bool CallingEvents = false;
        public static void FireCustomVerUtilsEvent(this Scene scene, string EventName)
        {
            EventCallBuffer.Enqueue(EventName);
            if (CallingEvents)
            {
                Logger.Log(LogLevel.Debug, "VerUtils",
                    $"Buffering named event: \"{EventName}\"");
                return;
            }
            CallingEvents = true;
            while (EventCallBuffer.Count > 0)
            {
                string CurrentEventRun = EventCallBuffer.Dequeue();
                Logger.Log(LogLevel.Debug, "VerUtils",
                    $"Fired named event: \"{CurrentEventRun}\"");
                int count = 0;
                foreach (
                    VerUtilsEventListener listener
                    in scene.Tracker.GetComponents<VerUtilsEventListener>()
                    )
                {
                    if (listener.Name != CurrentEventRun)
                        continue;
                    Logger.Log(LogLevel.Debug, "VerUtils",
                        $"Running event function for an entity of " +
                        $"type \"{listener.Entity.GetType()}\" " +
                        $"with position {listener.Entity.Position}");
                    listener.Run();
                    count++;
                }
                Logger.Log(LogLevel.Debug, "VerUtils",
                    $"Total number of listeners run: {count}");
            }
            CallingEvents = false;
        }

        public static void FireCustomVerUtilsEventsFromCondition(this Scene scene, EventFirer.Condition condition, bool immediately = false)
        {
            foreach (
                EventFirer firer
                in scene.Tracker.GetEntities<EventFirer>()
                )
            {
                firer.FireEvent(immediately);
            }
        }

        public static T PseudoTrackNearestTo<T>(this Scene scene, Vector2 position) where T : Entity
        {
            var list = scene.Tracker.GetEntities<T>();
            T ret = null;
            foreach (var check in list)
            {
                if (ret == null ||
                    (position - check.Position).LengthSquared() < (position - ret.Position).LengthSquared())
                {
                    ret = (T)check;
                    continue; 
                }
            }
            return ret;
        }
        #endregion

        #region Misc
        public static int WithTag(this int self, BitTag toAdd, bool On = true)
        {
            return (On ? self | toAdd : self & ~toAdd);
        }

        public static int WithTags(this int self, Dictionary<BitTag, bool> tags)
        {
            int ret = self;
            foreach(KeyValuePair<BitTag, bool> Set in tags)
            {
                ret = ret.WithTag(Set.Key, Set.Value);
            }
            return ret;
        }

        public static int WithTags(this int self, params BitTag[] tags)
        {
            int ret = self;
            foreach (BitTag tag in tags)
            {
                ret = ret.WithTag(tag);
            }
            return ret;
        }
        #endregion

        #endregion

        //Depth Definitions
        public static class Depths
        {
            //Note that the higher, the further it is
            //I'll comment all the vanilla depths in them
            //Y'know... for reference
            //Player is at depth 0 btw

            //BACK===================================
            public const int FirstUpdate = int.MaxValue; //Makes it so that it updates last.
            // - BGTerrain: BG Tiles = 10,000
            // - BGMirrors: Reflective BG Mirrors = 9,500
            // - BGDecals: BG Decals = 9,000
            // - BGParticles: BG Particles = 8,000
            // - SolidsBelow: Solids when set to BG mode = 5,000
            // - Below: Generic BG Entities = 2,000
            // - NPCs: Characters (People) = 1,000
            // - TheoCrystal: Theo (in Crystal) = 100
            public const int RailBooster_Rail_BG = 10;
            //PLAYER = 0  =====================================
            // - Dust: Dust Bunnies? = -50
            // - Pickups: Jellyfish? = -100
            // - Seeker: Seekers = -200
            // - Particles: FG Particles = -8,000
            // - Above: Generic FG Entities = -8,500
            // - Solids: Solid Blocks = -9,000
            // - FGTerrain: FG Tiles = -10,000
            public const int Rotary_FGTerrainBase_Idle = VanillaDepths.FGTerrain;
            // - FGDecals: FG Decals = -10,500
            // - DreamBlocks: Dream Blocks = -11,000
            public const int CrystalSpinners_FG = VanillaDepths.CrystalSpinners;
            // - CrystalSpinners: FG Crystal Spinners (Vestigial Vanilla) = -11,500
            // - PlayerDreamDashing: Going through a Dream Block = -12,000
            // - Enemy: Badeline Chasers = -12,500
            // - FakeWalls: Secret Paths = -13,000
            public const int RailBooster_Rail_FG = -25_499;
            public const int RailBooster_Node = -25_500;
            public const int RailBooster_Entry = -25_501;
            public const int RailBooster_Player = -25_502;
            // - FGParticles: FG Particles = -50,000
            public const int Reticle = -1_950_000;
            // - Top: Pseudo UI (Dash Assist and Grab Toggle) = -1,000,000
            // - FormationSequences: Heart Collection and Bubble Return = -2,000,000
            public const int LastUpdate = int.MinValue; //Makes it so that it updates first
            //FRONT==================================
        }

        #region Settings
        public static bool JokeMode => Settings.JokesAndTrolls;
        public static bool TrollsUnlocked => Settings.TrollsUnlocked;
        //List of various jokes included.
        public static class Jokes
        {
            //For messing with game logic, classifies as a troll due to the potential problems
            public static bool Technical => Settings.TrollTech && JokeMode && TrollsUnlocked;
            //Messing with visuals, like making madeline spin through transitions
            public static bool Visual => Settings.JokeGraphics && JokeMode;
            //Occasionally replace sounds with stupider ones
            public static bool Auditory => Settings.JokeAudio && JokeMode;
            //Pause screen eastereggs, like automatically having Tetris 4P CPU F4A
            public static bool InPause => Settings.JokePause && JokeMode;
            //Do weird stuff in the overworld, like making all snow deadpaneline
            public static bool OnMountain => Settings.JokeOverworld && JokeMode;
        }
        #endregion
    }
}
