using FMOD;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    // I want to make stuff easier to do-
    public sealed class HookPack : IDisposable
    {
        private HashSet<Hook> hooks = new();
        private HashSet<ILHook> ilhooks = new();
        private Dictionary<EventInfo, List<Delegate>> eventHandlers = new();
        public string LogName;

        public void Add(Hook hook)
        {
            MemberInfo info = hook.DetourInfo.Method.Method;
            Logger.Log(LogLevel.Debug, $"{LogName}/VerUtils/HookPack/On",
                $"Adding {info.DeclaringType}.{info.Name} to the pack.");
            hooks.Add(hook);
        }

        public void Add(ILHook ilhook)
        {
            var info = ilhook.HookInfo.ManipulatorMethod;
            Logger.Log(LogLevel.Debug, $"{LogName}/VerUtils/HookPack/IL",
                $"Adding {info.DeclaringType}.{info.Name} to the pack.");
            ilhooks.Add(ilhook);
        }

        public void Add(EventInfo Event, Delegate hook)
        {
            MethodInfo info = hook.GetMethodInfo();
            Logger.Log(LogLevel.Debug, $"{LogName}/VerUtils/HookPack/Events",
                $"Applying {info.DeclaringType}.{info.Name} to {Event.DeclaringType}.{Event.Name}");
            eventHandlers.TryAdd(Event, new());
            var handlers = eventHandlers[Event];
            Event.AddEventHandler(null, hook.CastDelegate(Event.EventHandlerType));
            handlers.Add(hook);
        }

        public void Add(Type type, string EventName, Delegate hook)
        {
            Add(type.GetEvent(EventName), hook);
        }

        public void Dispose() {
            Logger.Log(LogLevel.Debug, $"{LogName}/VerUtils/HookPack",
                "Disposing of hooks.");
            foreach (Hook hook in hooks)
                hook.Dispose();
            foreach(ILHook ilhook in ilhooks)
                ilhook.Dispose();
            foreach ((EventInfo Event, List<Delegate> handlers) in eventHandlers)
            {
                foreach (Delegate handler in handlers)
                {
                    Event.RemoveEventHandler(null, handler);
                }
            }
            hooks = new();
            ilhooks = new();
            eventHandlers = new();
        }
    }
}
