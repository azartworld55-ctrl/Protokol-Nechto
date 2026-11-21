namespace Protocol.Social.Memory;

public sealed record MemoryLifecycleResult(
    int EvaluatedEntries,
    int DecayedEntries,
    int ForgottenEntries,
    int EvictedEntries,
    int RetainedEntries);

/// <summary>
/// Applies deterministic decay and bounded retention to one resident-owned
/// journal. The service has no API for discovering other residents' memories.
/// </summary>
public sealed class MemoryLifecycleService
{
    public MemoryLifecycleResult Advance(
        MemoryJournal journal,
        long currentTick,
        MemoryLifecyclePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(policy);

        if (currentTick < 0)
            throw new ArgumentOutOfRangeException(nameof(currentTick));

        if (journal.LastLifecycleTick is long lastTick && currentTick < lastTick)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentTick),
                "Memory lifecycle time cannot move backwards, even when the journal is empty.");
        }

        var snapshot = journal.Entries;
        var replacements = new Dictionary<Guid, MemoryEntry>();
        var removals = new HashSet<Guid>();
        var retained = new List<MemoryEntry>(snapshot.Count);
        var decayedEntries = 0;
        var forgottenEntries = 0;

        foreach (var entry in snapshot)
        {
            if (currentTick < entry.LastEvaluatedTick)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentTick),
                    "Memory lifecycle time cannot move backwards relative to an entry.");
            }

            var elapsedTicks = currentTick - entry.LastEvaluatedTick;
            var decay = CalculateDecay(
                elapsedTicks,
                policy.RecallDecayPerTickPermille);
            var recallStrength = Math.Max(0, entry.RecallStrengthPermille - decay);

            if (recallStrength < entry.RecallStrengthPermille)
                decayedEntries++;

            var updated = entry.WithLifecycle(recallStrength, currentTick);
            var age = currentTick - entry.LearnedAtTick;

            if (age >= policy.MinimumAgeBeforeForgettingTicks &&
                recallStrength <= policy.ForgetBelowRecallStrengthPermille)
            {
                removals.Add(entry.EventId);
                forgottenEntries++;
                continue;
            }

            retained.Add(updated);
            if (updated.RecallStrengthPermille != entry.RecallStrengthPermille ||
                updated.LastEvaluatedTick != entry.LastEvaluatedTick)
            {
                replacements.Add(entry.EventId, updated);
            }
        }

        var overflow = retained.Count - policy.MaximumEntries;
        var evictedEntries = 0;
        if (overflow > 0)
        {
            var evictionOrder = retained
                .OrderBy(entry => entry.ImportancePermille)
                .ThenBy(entry => entry.RecallStrengthPermille)
                .ThenBy(entry => entry.LearnedAtTick)
                .ThenBy(entry => entry.EventId)
                .Take(overflow);

            foreach (var entry in evictionOrder)
            {
                if (removals.Add(entry.EventId))
                    evictedEntries++;
            }
        }

        journal.ApplyLifecycle(replacements, removals, currentTick);

        return new MemoryLifecycleResult(
            EvaluatedEntries: snapshot.Count,
            DecayedEntries: decayedEntries,
            ForgottenEntries: forgottenEntries,
            EvictedEntries: evictedEntries,
            RetainedEntries: journal.Count);
    }

    private static int CalculateDecay(long elapsedTicks, int decayPerTickPermille)
    {
        if (elapsedTicks == 0 || decayPerTickPermille == 0)
            return 0;

        // Recall strength cannot exceed 1000. Compare before multiplying so
        // long.MaxValue elapsed ticks cannot overflow.
        if (elapsedTicks > 1000L / decayPerTickPermille)
            return 1000;

        return (int)(elapsedTicks * decayPerTickPermille);
    }
}
