using System;
using System.Collections.Generic;
using Celeste;

namespace Celeste.Mod.Verillia.Utils {
    public class VerilliaUtilsSession : EverestModuleSession {

        //Settled Variables
        public Dictionary<string, bool> SettledFlags;
        public Dictionary<string, int> SettledInts;
        public Dictionary<string, float> SettledFloats;

        //Variable Flaglikes
        public Dictionary<string, int> Ints;
        public Dictionary<string, float> Floats;

        //TimeStates
        public enum TimeState
        {
            Play = 1,
            Pause = 0,
            Rewind = -1
        }
        public Dictionary<string, TimeState> TimeStates;

        //EventCalls
        public Dictionary<string, HashSet<Action>> Events;

        public void SettleFlags(Session session)
        {
            foreach(KeyValuePair<string, bool> i in SettledFlags)
            {
                if(i.Value){
                    session.Flags.Add(i.Key);
                    continue;
                }
                session.Flags.Remove(i.Key);
            }
        }

        public void SettleInts()
        {
            foreach (KeyValuePair<string, int> i in SettledInts)
            {
                Ints[i.Key] = i.Value;
            }
        }

        public void SettleFloats()
        {
            foreach (KeyValuePair<string, float> i in SettledFloats)
            {
                Floats[i.Key] = i.Value;
            }
        }

        public void Settle(Session session)
        {
            SettleFlags(session);
            SettleInts();
            SettleFloats();
        }


    }
}
