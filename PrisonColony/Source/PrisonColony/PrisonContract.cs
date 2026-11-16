using Verse;

namespace AzartWorld.PrisonColony
{
    public enum PrisonContractStatus
    {
        Offered = 0,
        Accepted = 1,
        Active = 2,
        Departing = 3,
        Closed = 4,
        Failed = 5
    }

    public sealed class PrisonContract : IExposable
    {
        public int Id = -1;
        public int PrisonerCount;
        public int DurationDays = 7;
        public int DaysElapsed;
        public int DailyRate = 10;
        public int Status;
        public int IntakeTick = -1;
        public int LastPaidDay = -1;
        public int RegisteredPrisoners;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id", -1);
            Scribe_Values.Look(ref PrisonerCount, "prisonerCount", 0);
            Scribe_Values.Look(ref DurationDays, "durationDays", 7);
            Scribe_Values.Look(ref DaysElapsed, "daysElapsed", 0);
            Scribe_Values.Look(ref DailyRate, "dailyRate", 10);
            Scribe_Values.Look(ref Status, "status", (int)PrisonContractStatus.Offered);
            Scribe_Values.Look(ref IntakeTick, "intakeTick", -1);
            Scribe_Values.Look(ref LastPaidDay, "lastPaidDay", -1);
            Scribe_Values.Look(ref RegisteredPrisoners, "registeredPrisoners", 0);
        }

        public PrisonContractStatus CurrentStatus
        {
            get { return (PrisonContractStatus)Status; }
            set { Status = (int)value; }
        }
    }
}
