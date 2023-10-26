using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontiers.Content.Flags {
    public static class FlagHandler {
        public static Dictionary<string, Flag> loadedFlags = new();

        public static void Handle(Flag flag) {
            flag.id = (short)loadedFlags.Count;
            loadedFlags.Add(flag.name, flag);
        }

        public static bool Has(this Flag[] otherFlags, Flag flag) {
            for (int i = 0; i < otherFlags.Length; i++) if (flag.Equals(otherFlags[i])) return true;
            return false;
        }

        public static bool HasAll(this Flag[] otherFlags, Flag[] flags) {
            // If it has less flags, it can't have them all
            if (otherFlags.Length < flags.Length) return false;

            int matches = 0;
            for (int i = 0; i < flags.Length; i++) {
                for (int j = 0; j < otherFlags.Length; j++) {

                    if (!flags[i].Equals(otherFlags[j])) continue;

                    matches++;
                    break;
                }
            }

            // Return true if all flags had a match
            return matches >= flags.Length;
        }

        public static bool HasAny(this Flag[] otherFlags, Flag[] flags) {
            for (int i = 0; i < flags.Length; i++) {
                for (int j = 0; j < otherFlags.Length; j++) {
                    if (flags[i].Equals(otherFlags[j])) return true;
                }
            }
            return false;
        }

        public static Flag[] GetMatches(this Flag[] otherFlags, Flag[] flags) {
            List<Flag> matchedFlags = new();

            for (int i = 0; i < flags.Length; i++) {
                for (int j = 0; j < otherFlags.Length; j++) {
                    if (!flags[i].Equals(otherFlags[j]) || matchedFlags.Contains(flags[i])) continue;

                    matchedFlags.Add(flags[i]);
                    break;
                }
            }

            return matchedFlags.ToArray();
        }

        public static bool Has(this IFlaggable other, Flag flag) {
            return other.GetFlags().Has(flag);
        }

        public static bool HasAll(this IFlaggable other, Flag[] flags) {
            return other.GetFlags().HasAll(flags);
        }

        public static bool HasAny(this IFlaggable other, Flag[] flags) {
            return other.GetFlags().HasAny(flags);
        }

        public static Flag[] GetMatches(this IFlaggable other, Flag[] flags) {
            return other.GetFlags().GetMatches(flags);
        }
    }

    public class Flag {
        public string name; // The display name of this flag
        public short id; // The id used to compare this flag

        public Flag(string name) {
            this.name = name;
            FlagHandler.Handle(this);
        }

        public bool Equals(Flag other) {
            return id == other.id;
        }
    }

    public static class FlagTypes {
        public static Flag 
            wall, core,
            aircraft, copter, mech, maxwell, 
            fighter, bomber, support, interceptor, 
            light, heavy,
            fast, slow, 
            lightArmored, moderateArmored, heavyArmored;

        public static void Load() {
            wall = new("wall");
            core = new("core");
            aircraft = new("aircraft");
            copter = new("copter");
            mech = new("mech");
            maxwell = new("maxwell");
            fighter = new("fighter");
            bomber = new("bomber");
            support = new("support");
            interceptor = new("interceptor");
            light = new("light");
            heavy = new("heavy");
            fast = new("fast");
            slow = new("slow");
            lightArmored = new("lightArmored");
            moderateArmored = new("moderateArmored");
            heavyArmored = new("heavyArmored");
        }
    }

    public interface IFlaggable {
        public Flag[] GetFlags();
    }
}

