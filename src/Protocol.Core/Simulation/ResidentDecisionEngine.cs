using Protocol.Core.Domain;
using Protocol.Core.Social;

namespace Protocol.Core.Simulation;

/// <summary>
/// Selects a plausible next activity from needs, personality, habits and
/// personally known information. It never consults hidden station-wide state.
/// </summary>
public sealed class ResidentDecisionEngine
{
    public ResidentAction ChooseNext(
        ResidentMind mind,
        IReadOnlyCollection<Resident> knownResidents,
        RelationshipGraph relationships,
        int hourOfDay)
    {
        ArgumentNullException.ThrowIfNull(mind);
        ArgumentNullException.ThrowIfNull(knownResidents);
        ArgumentNullException.ThrowIfNull(relationships);

        if (hourOfDay is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hourOfDay));

        var resident = mind.Resident;
        var needs = mind.Needs;
        var personality = resident.Personality;
        var scores = new Dictionary<ResidentAction, double>
        {
            [ResidentAction.Work] = 25 + personality.Discipline * 0.25,
            [ResidentAction.Eat] = needs.Hunger * 1.15,
            [ResidentAction.Rest] = needs.Fatigue * 0.65,
            [ResidentAction.Sleep] = hourOfDay is >= 22 or < 6 ? 75 + needs.Fatigue * 0.35 : 0,
            [ResidentAction.Socialize] = needs.Social * 0.65 + personality.Sociability * 0.25,
            [ResidentAction.Recreation] = needs.Stress * 0.8 + personality.Curiosity * 0.08,
            [ResidentAction.Inspect] = personality.Curiosity * 0.35,
            [ResidentAction.SeekQuiet] = personality.Caution * 0.2 + (100 - personality.Sociability) * 0.15
        };

        if (resident.Habits.Contains(ResidentHabit.EatsAtRegularHours) && hourOfDay is 7 or 12 or 18)
            scores[ResidentAction.Eat] += 18;
        if (resident.Habits.Contains(ResidentHabit.ReadsBeforeSleep) && hourOfDay is >= 20 or < 6)
            scores[ResidentAction.Recreation] += 12;
        if (resident.Habits.Contains(ResidentHabit.SocializesDuringBreaks) && hourOfDay is 10 or 15)
            scores[ResidentAction.Socialize] += 15;
        if (resident.Habits.Contains(ResidentHabit.PrefersQuiet))
            scores[ResidentAction.SeekQuiet] += 12;
        if (resident.Habits.Contains(ResidentHabit.WorksLate) && hourOfDay is >= 20 or < 2)
            scores[ResidentAction.Work] += 12;

        var trustedContact = knownResidents
            .Where(other => other.Id != resident.Id)
            .Select(other => relationships.Get(resident.Id, other.Id))
            .Select(state => state.Trust)
            .DefaultIfEmpty(0)
            .Max();
        scores[ResidentAction.Socialize] += Math.Max(0, trustedContact) * 0.08;

        if (mind.Memories.Entries.Any(entry =>
                entry.Origin == MemoryOrigin.Observation && entry.Confidence >= 0.7))
            scores[ResidentAction.Inspect] += personality.Curiosity * 0.05;

        // Stable tie-breaking makes simulation replays reproducible.
        return scores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .First()
            .Key;
    }
}
