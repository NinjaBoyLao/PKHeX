﻿using System.Collections.Generic;

namespace PKHeX.Core
{
    public static class LockFinder
    {
        public static bool IsAllShadowLockValid(EncounterStaticShadow s, PIDIV pv, PKM pkm)
        {
            var teams = s.Locks;
            if (teams.Length == 0)
                return true;

            var tsv = s.Version == GameVersion.XD ? pkm.TSV : -1; // no xd shiny shadow mons
            return IsAllShadowLockValid(pv, teams, tsv);
        }

        public static bool IsAllShadowLockValid(PIDIV pv, IEnumerable<TeamLock> teams, int tsv = -1)
        {
            foreach (var t in teams)
            {
                var result = new TeamLockResult(t, pv.OriginSeed, tsv);
                if (result.Valid)
                    return true;
            }
            return false;
        }

        public static bool IsFirstShadowLockValid(PIDIV pv, TeamLock[] teams)
        {
            if (teams.Length == 0)
                return true;

            var singleTeams = new TeamLock[teams.Length];
            for (int i = 0; i < teams.Length; i++)
            {
                var t = teams[i];
                var clone = t.Clone();
                var first = t.Locks[t.Locks.Length - 1];
                clone.Locks = new[] { first };
                singleTeams[i] = clone;
            }

            return IsAllShadowLockValid(pv, singleTeams);
        }

        // Colosseum/XD Starters
        public static bool IsXDStarterValid(uint seed, int TID, int SID)
        {
            // pidiv reversed 2x yields SID, 3x yields TID. shift by 7 if another PKM is generated prior
            var SIDf = RNG.XDRNG.Reverse(seed, 2);
            var TIDf = RNG.XDRNG.Prev(SIDf);
            return SIDf >> 16 == SID && TIDf >> 16 == TID;
        }

        public static bool IsColoStarterValid(int species, ref uint seed, int TID, int SID, uint pkPID, uint IV1, uint IV2)
        {
            // reverse the seed the bare minimum
            int rev = 2;
            if (species == 196)
                rev += 7;

            var rng = RNG.XDRNG;
            var SIDf = rng.Reverse(seed, rev);
            int ctr = 0;
            uint temp;
            while ((temp = rng.Prev(SIDf)) >> 16 != TID || SIDf >> 16 != SID)
            {
                SIDf = temp;
                if (ctr > 32) // arbitrary
                    return false;
                ctr++;
            }

            var next = rng.Next(SIDf);

            // generate Umbreon
            var PIDIV = GenerateValidColoStarterPID(ref next, TID, SID);
            if (species == 196) // need espeon, which is immediately next
                PIDIV = GenerateValidColoStarterPID(ref next, TID, SID);

            if (!PIDIV.Equals(pkPID, IV1, IV2))
                return false;
            seed = rng.Reverse(SIDf, 2);
            return true;
        }

        private struct PIDIVGroup
        {
            public uint PID;
            public uint IV1;
            public uint IV2;

            public bool Equals(uint pid, uint iv1, uint iv2) => PID == pid && IV1 == iv1 && IV2 == iv2;
        }

        private static PIDIVGroup GenerateValidColoStarterPID(ref uint uSeed, int TID, int SID)
        {
            var rng = RNG.XDRNG;
            PIDIVGroup group = new PIDIVGroup();

            uSeed = rng.Advance(uSeed, 2); // skip fakePID
            group.IV1 = (uSeed >> 16) & 0x7FFF;
            uSeed = rng.Next(uSeed);
            group.IV2 = (uSeed >> 16) & 0x7FFF;
            uSeed = rng.Next(uSeed);
            uSeed = rng.Advance(uSeed, 1); // skip ability call
            group.PID = GenerateStarterPID(ref uSeed, TID, SID);

            uSeed = rng.Advance(uSeed, 2); // PID calls consumed

            return group;
        }

        private static bool IsShiny(int TID, int SID, uint PID) => (TID ^ SID ^ (PID >> 16) ^ (PID & 0xFFFF)) < 8;

        private static uint GenerateStarterPID(ref uint uSeed, int TID, int SID)
        {
            uint PID;
            const byte ratio = 0x20; // 12.5% F (can't be female)
            while (true)
            {
                var next = RNG.XDRNG.Next(uSeed);
                PID = (uSeed & 0xFFFF0000) | (next >> 16);
                if ((PID & 0xFF) > ratio && !IsShiny(TID, SID, PID))
                    break;
                uSeed = RNG.XDRNG.Next(next);
            }

            return PID;
        }
    }
}
