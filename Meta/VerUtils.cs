using Celeste.Mod.Verillia.Utils;
using Celeste.Mod.Verillia.Utils.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
            Logger.Log(LogLevel.Debug, "VerUtils",
                "Adding Player Extension component to Player");
            player.Add(playerExt = new VerilliaUtilsPlayerExt());
            return playerExt;
        }
        #endregion

        #region Actor
        //For ease of getting countermovement component
        public static CounterMovement GetCounterMovement(this Actor actor)
        {
            CounterMovement ret = actor.Components.Get<CounterMovement>();
            if (ret is null)
                actor.Add(ret = new CounterMovement());
            return ret;
        }

        #region Riding
        public static bool IsRidingAnySolid(this Actor actor)
        {
            foreach (Solid c in actor.Scene.Tracker.GetEntities<Solid>())
            {
                if (actor.IsRiding(c))
                    return true;
            }
            return false;
        }

        public static bool IsRidingAnyJumpThru(this Actor actor)
        {
            foreach (JumpThru c in actor.Scene.Tracker.GetEntities<JumpThru>())
            {
                if (actor.IsRiding(c))
                    return true;
            }
            return false;
        }

        public static bool IsRidingAnySolidOrJumpThru(this Actor actor)
        {
            if (actor.IsRidingAnySolid())
                return true;
            return actor.IsRidingAnyJumpThru();
        }
        #endregion

        #region NaiveMove
        public static void NaiveMoveH(this Actor actor, float amount)
        {
            actor.NaiveMove(Vector2.UnitX * amount);
        }

        public static void NaiveMoveV(this Actor actor, float amount)
        {
            actor.NaiveMove(Vector2.UnitY * amount);
        }

        public static void NaiveMoveTo(this Actor actor, Vector2 goal)
        {
            Vector2 amount = Vector2.Zero;
            amount.X = (float)((double)goal.X - (double)actor.Position.X - (double)actor.movementCounter.X);
            amount.Y = (float)((double)goal.Y - (double)actor.Position.Y - (double)actor.movementCounter.Y);
            actor.NaiveMove(amount);
        }

        public static void NaiveMoveToX(this Actor actor, float goal)
        {
            actor.NaiveMoveTo(new Vector2(goal, actor.ExactPosition.Y));
        }

        public static void NaiveMoveToY(this Actor actor, float goal)
        {
            actor.NaiveMoveTo(new Vector2(actor.ExactPosition.X, goal));
        }

        public static void NaiveMoveTowards(this Actor actor, Vector2 goal, float maxAmount)
        {
            Vector2 to = Calc.Approach(actor.ExactPosition, goal, maxAmount);
            actor.NaiveMoveTo(to);
        }

        public static void NaiveMoveTowardsY(this Actor actor, float targetY, float maxAmount, Collision onCollide = null)
        {
            float toY = Calc.Approach(actor.ExactPosition.Y, targetY, maxAmount);
            actor.NaiveMoveToY(toY);
        }

        public static void NaiveMoveTowardsX(this Actor actor, float targetX, float maxAmount, Collision onCollide = null)
        {
            float toX = Calc.Approach(actor.ExactPosition.X, targetX, maxAmount);
            actor.NaiveMoveToX(toX);
        }
        #endregion

        #region MoveUntil
        public static bool MoveHExactUntil(this Actor actor, int moveH, Func<bool> stopCondition, Collision onCollide = null, Solid pusher = null)
        {
            Vector2 targetPosition = actor.Position + Vector2.UnitX * moveH;
            int num = Math.Sign(moveH);
            int num2 = 0;
            while (moveH != 0 && !stopCondition())
            {
                Solid solid = actor.CollideFirst<Solid>(actor.Position + Vector2.UnitX * num);
                if (solid != null)
                {
                    actor.movementCounter.X = 0f;
                    onCollide?.Invoke(new CollisionData
                    {
                        Direction = Vector2.UnitX * num,
                        Moved = Vector2.UnitX * num2,
                        TargetPosition = targetPosition,
                        Hit = solid,
                        Pusher = pusher
                    });
                    return true;
                }
                num2 += num;
                moveH -= num;
                actor.X += num;
            }
            return false;
        }

        public static bool MoveVExactUntil(this Actor actor, int moveV, Func<bool> stopCondition, Collision onCollide = null, Solid pusher = null)
        {
            Vector2 targetPosition = actor.Position + Vector2.UnitY * moveV;
            int num = Math.Sign(moveV);
            int num2 = 0;
            while (moveV != 0 && !stopCondition())
            {
                Solid solid = actor.CollideFirst<Solid>(actor.Position + Vector2.UnitY * num);
                if (solid != null)
                {
                    actor.movementCounter.X = 0f;
                    onCollide?.Invoke(new CollisionData
                    {
                        Direction = Vector2.UnitY * num,
                        Moved = Vector2.UnitY * num2,
                        TargetPosition = targetPosition,
                        Hit = solid,
                        Pusher = pusher
                    });
                    return true;
                }
                num2 += num;
                moveV -= num;
                actor.X += num;
            }
            return false;
        }

        public static bool MoveVUntil(this Actor actor, float moveV, Func<bool> stopCondition, Collision onCollide = null, Solid pusher = null)
        {
            actor.movementCounter.Y += moveV;
            int num = (int)Math.Round(actor.movementCounter.Y, MidpointRounding.ToEven);
            if (num != 0)
            {
                actor.movementCounter.Y -= num;
                return actor.MoveVExactUntil(num, stopCondition, onCollide, pusher);
            }
            return false;
        }

        public static bool MoveHUntil(this Actor actor, float moveH, Func<bool> stopCondition, Collision onCollide = null, Solid pusher = null)
        {
            actor.movementCounter.X += moveH;
            int num = (int)Math.Round(actor.movementCounter.X, MidpointRounding.ToEven);
            if (num != 0)
            {
                actor.movementCounter.X -= num;
                return actor.MoveHExactUntil(num, stopCondition, onCollide, pusher);
            }
            return false;
        }

        public static void MoveToXUntil(this Actor actor, float toX, Func<bool> stopCondition, Collision onCollide = null)
        {
            actor.MoveHUntil((float)((double)toX - (double)actor.Position.X - (double)actor.movementCounter.X), stopCondition, onCollide);
        }

        public static void MoveToYUntil(this Actor actor, float toY, Func<bool> stopCondition, Collision onCollide = null)
        {
            actor.MoveVUntil((float)((double)toY - (double)actor.Position.Y - (double)actor.movementCounter.Y), stopCondition, onCollide);
        }
        #endregion

        #region Naive Move Until
        public static void NaiveMoveHExactUntil(this Actor actor, int moveH, Func<bool> stopCondition)
        {
            Vector2 targetPosition = actor.Position + Vector2.UnitX * moveH;
            int num = Math.Sign(moveH);
            int num2 = 0;
            while (moveH != 0 && !stopCondition())
            {
                num2 += num;
                moveH -= num;
                actor.X += num;
            }
            return;
        }

        public static void NaiveMoveVExactUntil(this Actor actor, int moveV, Func<bool> stopCondition)
        {
            Vector2 targetPosition = actor.Position + Vector2.UnitY * moveV;
            int num = Math.Sign(moveV);
            int num2 = 0;
            while (moveV != 0 && !stopCondition())
            {
                num2 += num;
                moveV -= num;
                actor.X += num;
            }
            return;
        }

        public static void NaiveMoveVUntil(this Actor actor, float moveV, Func<bool> stopCondition)
        {
            actor.movementCounter.Y += moveV;
            int num = (int)Math.Round(actor.movementCounter.Y, MidpointRounding.ToEven);
            if (num != 0)
            {
                actor.movementCounter.Y -= num;
                actor.NaiveMoveVExactUntil(num, stopCondition);
            }
            return ;
        }

        public static void NaiveMoveHUntil(this Actor actor, float moveH, Func<bool> stopCondition)
        {
            actor.movementCounter.X += moveH;
            int num = (int)Math.Round(actor.movementCounter.X, MidpointRounding.ToEven);
            if (num != 0)
            {
                actor.movementCounter.X -= num;
                actor.NaiveMoveHExactUntil(num, stopCondition);
            }
            return;
        }

        public static void NaiveMoveToXUntil(this Actor actor, float toX, Func<bool> stopCondition)
        {
            actor.NaiveMoveHUntil((float)((double)toX - (double)actor.Position.X - (double)actor.movementCounter.X), stopCondition);
        }

        public static void NaiveMoveToYUntil(this Actor actor, float toY, Func<bool> stopCondition)
        {
            actor.NaiveMoveVUntil((float)((double)toY - (double)actor.Position.Y - (double)actor.movementCounter.Y), stopCondition);
        }
        #endregion
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
                    ret = check as T;
                    continue; 
                }
            }
            return ret;
        }
        #endregion

        #region BitTag
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

        #region SimpleCurve
        public static void RenderBetter(this SimpleCurve curve, Vector2 offset, Color color, int resolution)
        {
            Vector2 start = offset + curve.Begin;
            for (int i = 1; i <= resolution; i++)
            {
                Vector2 vector = offset + curve.GetPoint(Ease.Follow(Ease.QuadOut, Ease.QuadIn)((float)i / resolution));
                Draw.Line(start, vector, color);
                start = vector;
            }
        }

        public static void RenderBetter(this SimpleCurve curve, Vector2 offset, Color color, int resolution, float thickness)
        {
            Vector2 start = offset + curve.Begin;
            for (int i = 1; i <= resolution; i++)
            {
                Vector2 vector = offset + curve.GetPoint(Ease.Follow(Ease.QuadOut, Ease.QuadIn)((float)i / resolution));
                Draw.Line(start, vector, color, thickness);
                start = vector;
            }
        }

        public static void RenderBetter(this SimpleCurve curve, Color color, int resolution)
        {
            curve.RenderBetter(Vector2.Zero, color, resolution);
        }

        public static void RenderBetter(this SimpleCurve curve, Color color, int resolution, float thickness)
        {
            curve.RenderBetter(Vector2.Zero, color, resolution, thickness);
        }
        #endregion

        #region Entity
        public static void PreUpdate(this Entity entity)
        {
            var Components = entity.Components;
            var trueLockMode = Components.LockMode;
            Components.LockMode = ComponentList.LockModes.Locked;
            foreach (ComplexComponent component in Components.GetAll<ComplexComponent>())
            {
                if (!component.Active)
                {
                    component.WasActive = false;
                    continue;
                }
                component.WasActive = true;
                component.PreUpdate();
            }
            Components.LockMode = trueLockMode;
        }

        public static void PostUpdate(this Entity entity)
        {
            var Components = entity.Components;
            var trueLockMode = Components.LockMode;
            Components.LockMode = ComponentList.LockModes.Locked;
            foreach (ComplexComponent component in Components.GetAll<ComplexComponent>())
            {
                if (!component.WasActive)
                    continue;
                component.PostUpdate();
            }
            Components.LockMode = trueLockMode;
        }

        public static void PreRender(this Entity entity)
        {
            var Components = entity.Components;
            var trueLockMode = Components.LockMode;
            Components.LockMode = ComponentList.LockModes.Locked;
            foreach (ComplexComponent component in Components.GetAll<ComplexComponent>())
            {
                if (!component.Visible)
                {
                    component.WasVisible = false;
                    continue;
                }
                component.WasVisible = true;
                component.PreRender();
            }
            Components.LockMode = trueLockMode;
        }

        public static void PostRender(this Entity entity)
        {
            var Components = entity.Components;
            var trueLockMode = Components.LockMode;
            Components.LockMode = ComponentList.LockModes.Locked;
            foreach (ComplexComponent component in Components.GetAll<ComplexComponent>())
            {
                if (!component.WasVisible)
                    continue;
                component.PostRender();
            }
            Components.LockMode = trueLockMode;
        }
        #endregion

        #region IL
        #endregion

        #region Type
        public static FieldInfo GetFieldInheritance(this Type type, string Name)
        {
            for (var Current = type; Current is not null; Current = Current.BaseType)
            {
                var ret = Current.GetField(Name);
                if (ret is not null)
                    return ret;
            }
            return null;
        }

        public static FieldInfo GetFieldInheritance(this Type type, string Name, BindingFlags bindingAttr)
        {
            for (var Current = type; Current is not null; Current = Current.BaseType)
            {
                var ret = Current.GetField(Name, bindingAttr);
                if (ret is not null)
                    return ret;
            }
            return null;
        }

        public static PropertyInfo GetPropertyInheritance(this Type type, string Name)
        {
            for (var Current = type; Current is not null; Current = Current.BaseType)
            {
                var ret = Current.GetProperty(Name);
                if (ret is not null)
                    return ret;
            }
            return null;
        }

        public static PropertyInfo GetPropertyInheritance(this Type type, string Name, BindingFlags bindingAttr)
        {
            for (var Current = type; Current is not null; Current = Current.BaseType)
            {
                var ret = Current.GetProperty(Name, bindingAttr);
                if (ret is not null)
                    return ret;
            }
            return null;
        }

        public static MemberInfo GetFieldOrPropertyInheritance(this Type type, string Name)
        {
            for (var Current = type; Current is not null; Current = Current.BaseType)
            {
                MemberInfo ret = Current.GetProperty(Name);
                if (ret is not null)
                    return ret;
                ret = Current.GetField(Name);
                if (ret is not null)
                    return ret;
            }
            return null;
        }

        public static MemberInfo GetFieldOrPropertyInheritance(this Type type, string Name, BindingFlags bindingAttr)
        {
            for (var Current = type; Current is not null; Current = Current.BaseType)
            {
                MemberInfo ret = Current.GetProperty(Name, bindingAttr);
                if (ret is not null)
                    return ret;
                ret = Current.GetField(Name, bindingAttr);
                if (ret is not null)
                    return ret;
            }
            return null;
        }

        public static object GetValueOfMember<From>(this From obj, string Name)
        {
            var info = typeof(From).GetFieldOrPropertyInheritance(Name);
            if (info is PropertyInfo property)
            {
                return property.GetValue(obj, null);
            }
            if (info is FieldInfo field)
            {
                return field.GetValue(obj);
            }
            return null;
        }

        public static object GetValueOfMember<From>(this From obj, string Name, BindingFlags bindingAttr)
        {
            var info = typeof(From).GetFieldOrPropertyInheritance(Name, bindingAttr);
            if (info is PropertyInfo property)
            {
                return property.GetValue(obj, null);
            }
            if (info is FieldInfo field)
            {
                return field.GetValue(obj);
            }
            return null;
        }

        public static object GetValueOfMember(this object obj, string Name)
        {
            var info = obj.GetType().GetFieldOrPropertyInheritance(Name);
            if (info is PropertyInfo property)
            {
                return property.GetValue(obj, null);
            }
            if (info is FieldInfo field)
            {
                return field.GetValue(obj);
            }
            return null;
        }

        public static object GetValueOfMember(this object obj, string Name, BindingFlags bindingAttr)
        {
            var info = obj.GetType().GetFieldOrPropertyInheritance(Name, bindingAttr);
            if (info is PropertyInfo property)
            {
                return property.GetValue(obj, null);
            }
            if (info is FieldInfo field)
            {
                return field.GetValue(obj);
            }
            return null;
        }

        public static void SetValueOfMember<From>(this From obj, string Name, object value)
        {
            var info = typeof(From).GetFieldOrPropertyInheritance(Name);
            if (info is PropertyInfo property)
            {
                property.SetValue(obj, value);
                return;
            }
            if (info is FieldInfo field)
            {
                field.SetValue(obj, value);
            }
            return;
        }

        public static void SetValueOfMember<From>(this From obj, string Name, object value, BindingFlags bindingAttr)
        {
            var info = typeof(From).GetFieldOrPropertyInheritance(Name, bindingAttr);
            if (info is PropertyInfo property)
            {
                property.SetValue(obj, value);
                return;
            }
            if (info is FieldInfo field)
            {
                field.SetValue(obj, value);
            }
            return;
        }

        public static void SetValueOfMember(this object obj, string Name, object value)
        {
            var info = obj.GetType().GetFieldOrPropertyInheritance(Name);
            if (info is PropertyInfo property)
            {
                property.SetValue(obj, value);
                return;
            }
            if (info is FieldInfo field)
            {
                field.SetValue(obj, value);
            }
            return;
        }

        public static void SetValueOfMember(this object obj, string Name, BindingFlags bindingAttr, object value)
        {
            var info = obj.GetType().GetFieldOrPropertyInheritance(Name, bindingAttr);
            if (info is PropertyInfo property)
            {
                property.SetValue(obj, value);
                return;
            }
            if (info is FieldInfo field)
            {
                field.SetValue(obj, value);
            }
            return;
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
            public const int RailBooster_Rail_BG = 2_010;
            // - Below: Generic BG Entities = 2,000
            // - NPCs: Characters (People) = 1,000
            // - TheoCrystal: Theo (in Crystal) = 100
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

    //So I don't get fucking confused.
    public static class Directions
    {
        public const int Y_Up = -1;
        public const int Y_Down = 1;
        public const int X_Left = -1;
        public const int X_Right = 1;

        public static Vector2 Up => Vector2.UnitY * Y_Up;
        public static Vector2 Down => Vector2.UnitY * Y_Down;
        public static Vector2 Left => Vector2.UnitX * X_Left;
        public static Vector2 Right => Vector2.UnitX * X_Right;
    }
}
