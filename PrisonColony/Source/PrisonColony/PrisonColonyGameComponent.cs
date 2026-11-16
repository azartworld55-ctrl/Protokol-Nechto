using System.Collections.Generic;
using Verse;

namespace AzartWorld.PrisonColony
{
    /// <summary>
    /// Persistent prison-management state. Contract accounting is independent
    /// of the transport adapter so it can be tested before shuttle integration.
    /// </summary>
    public sealed class PrisonColonyGameComponent : GameComponent
    {
        private const int TicksPerDay = 60000;

        public List<PrisonerRecord> Prisoners = new List<PrisonerRecord>();
        public List<PrisonContract> Contracts = new List<PrisonContract>();
        public int SchemaVersion = 2;
        public int Credits;
        public int NextContractId = 1;
        public int LastProcessedDay = -1;

        public PrisonColonyGameComponent(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SchemaVersion, "pc_schemaVersion", 2);
            Scribe_Values.Look(ref Credits, "pc_credits", 250);
            Scribe_Values.Look(ref NextContractId, "pc_nextContractId", 1);
            Scribe_Values.Look(ref LastProcessedDay, "pc_lastProcessedDay", -1);
            Scribe_Collections.Look(ref Prisoners, "pc_prisoners", LookMode.Deep);
            Scribe_Collections.Look(ref Contracts, "pc_contracts", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Prisoners == null) Prisoners = new List<PrisonerRecord>();
                if (Contracts == null) Contracts = new List<PrisonContract>();
                if (NextContractId < 1) NextContractId = 1;
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager == null) return;

            int currentDay = Find.TickManager.TicksGame / TicksPerDay;
            if (currentDay == LastProcessedDay) return;

            if (LastProcessedDay < 0)
            {
                LastProcessedDay = currentDay;
                return;
            }

            while (LastProcessedDay < currentDay)
            {
                LastProcessedDay++;
                ProcessDailyContracts(LastProcessedDay);
            }
        }

        public PrisonContract CreateOffer(int prisonerCount, int durationDays, int dailyRate)
        {
            if (prisonerCount < 1 || prisonerCount > 8) return null;
            if (durationDays < 1 || durationDays > 30) return null;
            if (dailyRate < 1) return null;

            PrisonContract contract = new PrisonContract
            {
                Id = NextContractId++,
                PrisonerCount = prisonerCount,
                DurationDays = durationDays,
                DailyRate = dailyRate,
                Status = (int)PrisonContractStatus.Offered
            };
            Contracts.Add(contract);
            return contract;
        }

        public bool AcceptOffer(int contractId, int availableCapacity)
        {
            PrisonContract contract = FindContract(contractId);
            if (contract == null || contract.CurrentStatus != PrisonContractStatus.Offered)
                return false;
            if (availableCapacity < contract.PrisonerCount)
                return false;

            contract.CurrentStatus = PrisonContractStatus.Accepted;
            return true;
        }

        /// <summary>
        /// Called by the arrival adapter after a pawn actually reaches intake.
        /// Contract time and payment do not start before the full party is registered.
        /// </summary>
        public bool RegisterArrival(int contractId, Pawn pawn)
        {
            PrisonContract contract = FindContract(contractId);
            if (contract == null || contract.CurrentStatus != PrisonContractStatus.Accepted)
                return false;
            if (pawn == null || !pawn.Spawned || !pawn.RaceProps.Humanlike || FindPrisoner(pawn) != null)
                return false;
            if (contract.RegisteredPrisoners >= contract.PrisonerCount)
                return false;

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            Prisoners.Add(new PrisonerRecord
            {
                Pawn = pawn,
                ContractId = contractId,
                IntakeTick = now,
                RemainingDays = contract.DurationDays,
                Category = "Standard"
            });

            contract.RegisteredPrisoners++;
            if (contract.RegisteredPrisoners >= contract.PrisonerCount)
            {
                contract.IntakeTick = now;
                contract.LastPaidDay = now / TicksPerDay;
                contract.CurrentStatus = PrisonContractStatus.Active;
            }
            return true;
        }

        public bool MarkDeparture(int contractId, Pawn pawn)
        {
            PrisonContract contract = FindContract(contractId);
            PrisonerRecord record = FindPrisoner(pawn);
            if (contract == null || record == null || record.ContractId != contractId)
                return false;
            if (contract.CurrentStatus != PrisonContractStatus.Departing)
                return false;

            Prisoners.Remove(record);
            contract.RegisteredPrisoners--;
            if (contract.RegisteredPrisoners <= 0)
            {
                contract.RegisteredPrisoners = 0;
                contract.CurrentStatus = PrisonContractStatus.Closed;
            }
            return true;
        }

        public PrisonContract FindContract(int contractId)
        {
            return Contracts.Find(c => c != null && c.Id == contractId);
        }

        public PrisonerRecord FindPrisoner(Pawn pawn)
        {
            return Prisoners.Find(p => p != null && p.Pawn == pawn);
        }

        private void ProcessDailyContracts(int currentDay)
        {
            foreach (PrisonContract contract in Contracts)
            {
                if (contract == null || contract.CurrentStatus != PrisonContractStatus.Active)
                    continue;
                if (contract.LastPaidDay >= currentDay) continue;

                int housed = CountPrisoners(contract.Id);
                if (housed > 0)
                    Credits += housed * contract.DailyRate;

                contract.LastPaidDay = currentDay;
                contract.DaysElapsed = currentDay - (contract.IntakeTick / TicksPerDay);

                foreach (PrisonerRecord prisoner in Prisoners)
                {
                    if (prisoner != null && prisoner.ContractId == contract.Id)
                        prisoner.RemainingDays = contract.DurationDays - contract.DaysElapsed;
                }

                if (contract.DaysElapsed >= contract.DurationDays)
                    contract.CurrentStatus = PrisonContractStatus.Departing;
            }
        }

        private int CountPrisoners(int contractId)
        {
            int count = 0;
            foreach (PrisonerRecord prisoner in Prisoners)
                if (prisoner != null && prisoner.ContractId == contractId && prisoner.Pawn != null && prisoner.Pawn.Spawned && !prisoner.Pawn.Destroyed)
                    count++;
            return count;
        }
    }

    public sealed class PrisonerRecord : IExposable
    {
        public Pawn Pawn;
        public int ContractId = -1;
        public int IntakeTick;
        public int RemainingDays;
        public string Category = "Standard";

        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn");
            Scribe_Values.Look(ref ContractId, "contractId", -1);
            Scribe_Values.Look(ref IntakeTick, "intakeTick", 0);
            Scribe_Values.Look(ref RemainingDays, "remainingDays", 0);
            Scribe_Values.Look(ref Category, "category", "Standard");
        }
    }
}
