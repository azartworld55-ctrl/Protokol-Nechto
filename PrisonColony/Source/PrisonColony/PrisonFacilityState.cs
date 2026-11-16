using System.Collections.Generic;
using Verse;

namespace AzartWorld.PrisonColony
{
    public enum FacilityModuleType
    {
        CellBlock = 0,
        SecurityCamera = 1,
        ControlConsole = 2,
        SecureDoor = 3,
        AlarmSystem = 4,
        Generator = 5,
        MedicalRoom = 6,
        Kitchen = 7,
        Sanitation = 8,
        RecreationRoom = 9
    }

    public enum PrisonSecurityMode
    {
        Normal = 0,
        IncreasedAttention = 1,
        Quarantine = 2,
        Emergency = 3
    }

    public enum PrisonStaffRole
    {
        Guard = 0,
        Warden = 1,
        Medic = 2,
        Cook = 3,
        Technician = 4
    }

    /// <summary>
    /// Abstract management layer for facility systems. It does not pretend to
    /// spawn map buildings; physical building integration is a later adapter.
    /// </summary>
    public sealed class PrisonFacilityState : IExposable
    {
        public List<FacilityModuleRecord> Modules = new List<FacilityModuleRecord>();
        public List<PrisonStaffRecord> Staff = new List<PrisonStaffRecord>();
        public int SecurityMode;
        public int Tension;
        public int LastMaintenanceDay = -1;
        public int LastPowerCheckDay = -1;
        public int CachedPowerDemand;
        public int CachedPowerCapacity;

        public PrisonSecurityMode CurrentSecurityMode
        {
            get { return (PrisonSecurityMode)SecurityMode; }
            set { SecurityMode = (int)value; }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Modules, "modules", LookMode.Deep);
            Scribe_Collections.Look(ref Staff, "staff", LookMode.Deep);
            Scribe_Values.Look(ref SecurityMode, "securityMode", (int)PrisonSecurityMode.Normal);
            Scribe_Values.Look(ref Tension, "tension", 0);
            Scribe_Values.Look(ref LastMaintenanceDay, "lastMaintenanceDay", -1);
            Scribe_Values.Look(ref LastPowerCheckDay, "lastPowerCheckDay", -1);
            Scribe_Values.Look(ref CachedPowerDemand, "cachedPowerDemand", 0);
            Scribe_Values.Look(ref CachedPowerCapacity, "cachedPowerCapacity", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Modules == null) Modules = new List<FacilityModuleRecord>();
                if (Staff == null) Staff = new List<PrisonStaffRecord>();
            }
        }

        public bool InstallModule(FacilityModuleType type, ref int credits)
        {
            int cost;
            int demand;
            int upkeep;
            ModuleStats(type, out cost, out demand, out upkeep);
            if (credits < cost) return false;

            credits -= cost;
            Modules.Add(new FacilityModuleRecord
            {
                Type = (int)type,
                Integrity = 100,
                PowerDemand = demand,
                DailyUpkeep = upkeep,
                Operational = type == FacilityModuleType.Generator
            });
            RecalculatePower();
            return true;
        }

        public bool AssignStaff(Pawn pawn, PrisonStaffRole role)
        {
            if (pawn == null || !pawn.Spawned || !pawn.RaceProps.Humanlike)
                return false;
            if (IsPrisoner(pawn)) return false;

            PrisonStaffRecord existing = Staff.Find(s => s != null && s.Pawn == pawn);
            if (existing != null)
            {
                existing.Role = (int)role;
                existing.Active = true;
                return true;
            }

            Staff.Add(new PrisonStaffRecord { Pawn = pawn, Role = (int)role, Active = true });
            return true;
        }

        public bool RemoveStaff(Pawn pawn)
        {
            PrisonStaffRecord record = Staff.Find(s => s != null && s.Pawn == pawn);
            if (record == null) return false;
            Staff.Remove(record);
            return true;
        }

        public void SetSecurityMode(PrisonSecurityMode mode)
        {
            CurrentSecurityMode = mode;
        }

        public int CountModules(FacilityModuleType type)
        {
            int count = 0;
            foreach (FacilityModuleRecord module in Modules)
                if (module != null && module.Type == (int)type)
                    count++;
            return count;
        }

        public int CountStaff(PrisonStaffRole role)
        {
            int count = 0;
            foreach (PrisonStaffRecord staff in Staff)
                if (staff != null && staff.Active && staff.Pawn != null &&
                    staff.Pawn.Spawned && !staff.Pawn.Destroyed && staff.Role == (int)role)
                    count++;
            return count;
        }

        public int GetSecurityRating()
        {
            int rating = CountStaff(PrisonStaffRole.Guard) * 12;
            rating += CountModules(FacilityModuleType.SecurityCamera) * 8;
            rating += CountModules(FacilityModuleType.SecureDoor) * 10;
            rating += CountModules(FacilityModuleType.AlarmSystem) * 8;
            rating += CountModules(FacilityModuleType.ControlConsole) * 6;
            if (CurrentSecurityMode == PrisonSecurityMode.IncreasedAttention) rating += 10;
            if (CurrentSecurityMode == PrisonSecurityMode.Quarantine) rating += 15;
            if (CurrentSecurityMode == PrisonSecurityMode.Emergency) rating += 20;
            return rating > 100 ? 100 : rating;
        }

        public int GetDailyUpkeep()
        {
            int total = 0;
            foreach (FacilityModuleRecord module in Modules)
                if (module != null && module.Integrity > 0)
                    total += module.DailyUpkeep;
            total += CountStaff(PrisonStaffRole.Guard) * 4;
            total += CountStaff(PrisonStaffRole.Warden) * 5;
            total += CountStaff(PrisonStaffRole.Medic) * 5;
            total += CountStaff(PrisonStaffRole.Cook) * 3;
            total += CountStaff(PrisonStaffRole.Technician) * 4;
            if (CurrentSecurityMode == PrisonSecurityMode.IncreasedAttention) total += 3;
            if (CurrentSecurityMode == PrisonSecurityMode.Quarantine) total += 5;
            if (CurrentSecurityMode == PrisonSecurityMode.Emergency) total += 8;
            return total;
        }

        public void ProcessDaily(ref int credits, int currentDay, int prisonerCount)
        {
            if (LastMaintenanceDay >= currentDay) return;
            if (LastMaintenanceDay < 0)
            {
                LastMaintenanceDay = currentDay;
                RecalculatePower();
                return;
            }

            int elapsed = currentDay - LastMaintenanceDay;
            for (int day = 0; day < elapsed; day++)
            {
                credits -= GetDailyUpkeep();
                RecalculatePower();

                if (GetSecurityRating() < 25 && prisonerCount > 0)
                    Tension += 2;
                else if (GetSecurityRating() >= 60)
                    Tension -= 2;

                if (CurrentSecurityMode == PrisonSecurityMode.Emergency)
                    Tension -= 3;

                if (Tension < 0) Tension = 0;
                if (Tension > 100) Tension = 100;
            }
            LastMaintenanceDay = currentDay;
        }

        private void RecalculatePower()
        {
            int capacity = CountModules(FacilityModuleType.Generator) * 60;
            int demand = 0;
            foreach (FacilityModuleRecord module in Modules)
            {
                if (module == null) continue;
                if (module.Type != (int)FacilityModuleType.Generator && module.Integrity > 0)
                    demand += module.PowerDemand;
            }

            CachedPowerCapacity = capacity;
            CachedPowerDemand = demand;
            int remaining = capacity;
            foreach (FacilityModuleRecord module in Modules)
            {
                if (module == null || module.Type == (int)FacilityModuleType.Generator) continue;
                bool powered = module.Integrity > 0 && remaining >= module.PowerDemand;
                module.Operational = powered;
                if (powered) remaining -= module.PowerDemand;
            }
            foreach (FacilityModuleRecord module in Modules)
                if (module != null && module.Type == (int)FacilityModuleType.Generator)
                    module.Operational = module.Integrity > 0;
        }

        private bool IsPrisoner(Pawn pawn)
        {
            // Prisoners are filtered by the owning game component before staff assignment.
            return false;
        }

        private static void ModuleStats(FacilityModuleType type, out int cost, out int demand, out int upkeep)
        {
            cost = 100;
            demand = 0;
            upkeep = 1;
            switch (type)
            {
                case FacilityModuleType.CellBlock: cost = 180; demand = 2; upkeep = 2; break;
                case FacilityModuleType.SecurityCamera: cost = 90; demand = 4; upkeep = 1; break;
                case FacilityModuleType.ControlConsole: cost = 160; demand = 5; upkeep = 2; break;
                case FacilityModuleType.SecureDoor: cost = 120; demand = 3; upkeep = 1; break;
                case FacilityModuleType.AlarmSystem: cost = 130; demand = 4; upkeep = 2; break;
                case FacilityModuleType.Generator: cost = 220; demand = 0; upkeep = 3; break;
                case FacilityModuleType.MedicalRoom: cost = 170; demand = 3; upkeep = 2; break;
                case FacilityModuleType.Kitchen: cost = 150; demand = 4; upkeep = 2; break;
                case FacilityModuleType.Sanitation: cost = 140; demand = 2; upkeep = 2; break;
                case FacilityModuleType.RecreationRoom: cost = 110; demand = 2; upkeep = 1; break;
            }
        }
    }

    public sealed class FacilityModuleRecord : IExposable
    {
        public int Type;
        public int Integrity = 100;
        public int PowerDemand;
        public int DailyUpkeep;
        public bool Operational;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Type, "type", 0);
            Scribe_Values.Look(ref Integrity, "integrity", 100);
            Scribe_Values.Look(ref PowerDemand, "powerDemand", 0);
            Scribe_Values.Look(ref DailyUpkeep, "dailyUpkeep", 1);
            Scribe_Values.Look(ref Operational, "operational", false);
        }
    }

    public sealed class PrisonStaffRecord : IExposable
    {
        public Pawn Pawn;
        public int Role;
        public bool Active = true;

        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn");
            Scribe_Values.Look(ref Role, "role", 0);
            Scribe_Values.Look(ref Active, "active", true);
        }
    }
}
