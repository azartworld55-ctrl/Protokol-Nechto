using System.Collections.Generic;
using Verse;

namespace AzartWorld.PrisonColony
{
    /// <summary>
    /// Persistent world-level state for the prison simulation.
    /// Kept deliberately small until Ports1de save/load behaviour is verified.
    /// </summary>
    public sealed class PrisonColonyGameComponent : GameComponent
    {
        public List<PrisonerRecord> Prisoners = new List<PrisonerRecord>();
        public List<PrisonContract> Contracts = new List<PrisonContract>();
        public int SchemaVersion = 1;

        public PrisonColonyGameComponent(Game game) : base(game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref SchemaVersion, "pc_schemaVersion", 1);
            Scribe_Collections.Look(ref Prisoners, "pc_prisoners", LookMode.Deep);
            Scribe_Collections.Look(ref Contracts, "pc_contracts", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Prisoners == null) Prisoners = new List<PrisonerRecord>();
                if (Contracts == null) Contracts = new List<PrisonContract>();
            }
        }
    }

    public sealed class PrisonerRecord : IExposable
    {
        public Pawn Pawn;
        public int ContractId;
        public int RemainingDays;
        public string Category = "Standard";

        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "pawn");
            Scribe_Values.Look(ref ContractId, "contractId", -1);
            Scribe_Values.Look(ref RemainingDays, "remainingDays", 0);
            Scribe_Values.Look(ref Category, "category", "Standard");
        }
    }

    public sealed class PrisonContract : IExposable
    {
        public int Id;
        public int PrisonerCount;
        public int DurationDays;
        public int DaysElapsed;
        public int DailyRate;
        public int Status;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id", -1);
            Scribe_Values.Look(ref PrisonerCount, "prisonerCount", 0);
            Scribe_Values.Look(ref DurationDays, "durationDays", 1);
            Scribe_Values.Look(ref DaysElapsed, "daysElapsed", 0);
            Scribe_Values.Look(ref DailyRate, "dailyRate", 0);
            Scribe_Values.Look(ref Status, "status", 0);
        }
    }
}
