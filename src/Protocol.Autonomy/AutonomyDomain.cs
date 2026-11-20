using System.Collections.ObjectModel;
using Protocol.Social.Domain;

namespace Protocol.Autonomy;

public enum NeedKind { Hunger = 0, Fatigue = 1, Social = 2, Stress = 3 }
public enum PersonalityAxis { Sociability = 0, Discipline = 1 }
public enum ScheduleActivity { Work = 0, Sleep = 1, Socialize = 2, Leisure = 3 }
public enum AutonomyActionKind
{
    Eat = 0, Sleep = 1, SeekSocialContact = 2, Decompress = 3,
    Work = 4, Leisure = 5, FreeTime = 6
}

/// <summary>Unmet needs use an inclusive 0..1000 scale; balance is supplied by policy.</summary>
public sealed record NeedState
{
    public const int Maximum = 1000;

    public NeedState(int hunger, int fatigue, int social, int stress)
    {
        Hunger = Validate(hunger, nameof(hunger));
        Fatigue = Validate(fatigue, nameof(fatigue));
        Social = Validate(social, nameof(social));
        Stress = Validate(stress, nameof(stress));
    }

    public int Hunger { get; }
    public int Fatigue { get; }
    public int Social { get; }
    public int Stress { get; }

    public int Get(NeedKind kind) => kind switch
    {
        NeedKind.Hunger => Hunger,
        NeedKind.Fatigue => Fatigue,
        NeedKind.Social => Social,
        NeedKind.Stress => Stress,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public NeedState Apply(NeedDelta delta) =>
        new(
            ApplyDelta(Hunger, delta.Hunger),
            ApplyDelta(Fatigue, delta.Fatigue),
            ApplyDelta(Social, delta.Social),
            ApplyDelta(Stress, delta.Stress));

    private static int Validate(int value, string name)
    {
        if (value is < 0 or > Maximum)
            throw new ArgumentOutOfRangeException(name, "Need values must be between 0 and 1000.");
        return value;
    }

    private static int ApplyDelta(int current, long delta)
    {
        if (delta > Maximum - current) return Maximum;
        if (delta < -current) return 0;
        return (int)(current + delta);
    }
}

/// <summary>Signed per-hour changes; positive increases unmet need, negative satisfies it.</summary>
public readonly record struct NeedDelta(long Hunger, long Fatigue, long Social, long Stress);

/// <summary>A recurring daily window. Duration may cross midnight; end is exclusive.</summary>
public sealed record ScheduleBlock
{
    public ScheduleBlock(int startMinute, int durationMinutes, ScheduleActivity activity)
    {
        if (startMinute is < 0 or >= 1440)
            throw new ArgumentOutOfRangeException(nameof(startMinute));
        if (durationMinutes is < 1 or > 1440)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (!Enum.IsDefined(activity))
            throw new ArgumentOutOfRangeException(nameof(activity));

        StartMinute = startMinute;
        DurationMinutes = durationMinutes;
        Activity = activity;
    }

    public int StartMinute { get; }
    public int DurationMinutes { get; }
    public ScheduleActivity Activity { get; }

    internal bool Contains(int minuteOfDay) =>
        (minuteOfDay - StartMinute + 1440) % 1440 < DurationMinutes;
}

/// <summary>Unassigned minutes are free time. Overlaps, including midnight overlaps, are rejected.</summary>
public sealed class DailySchedule
{
    public DailySchedule(IEnumerable<ScheduleBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        var source = blocks.ToArray();
        if (source.Any(block => block is null))
            throw new ArgumentException("Schedule blocks cannot contain null.", nameof(blocks));

        var occupied = new bool[1440];
        foreach (var block in source)
        {
            for (var offset = 0; offset < block.DurationMinutes; offset++)
            {
                var minute = (block.StartMinute + offset) % 1440;
                if (occupied[minute])
                    throw new ArgumentException("Daily schedule blocks cannot overlap.", nameof(blocks));
                occupied[minute] = true;
            }
        }

        Blocks = Array.AsReadOnly(source
            .OrderBy(block => block.StartMinute)
            .ThenBy(block => block.DurationMinutes)
            .ToArray());
    }

    public IReadOnlyList<ScheduleBlock> Blocks { get; }

    public ScheduleActivity? GetActivityAt(int minuteOfDay)
    {
        if (minuteOfDay is < 0 or >= 1440)
            throw new ArgumentOutOfRangeException(nameof(minuteOfDay));

        foreach (var block in Blocks)
            if (block.Contains(minuteOfDay))
                return block.Activity;

        return null;
    }
}

/// <summary>Minimal immutable input that a future Core/game adapter can populate.</summary>
public sealed class ResidentAutonomyProfile
{
    private readonly HashSet<string> _habitLookup;

    public ResidentAutonomyProfile(
        ResidentId id,
        string professionId,
        int sociability,
        int discipline,
        IEnumerable<string> habitIds)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (string.IsNullOrWhiteSpace(professionId))
            throw new ArgumentException("Profession id is required.", nameof(professionId));
        ArgumentNullException.ThrowIfNull(habitIds);
        if (sociability is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(sociability));
        if (discipline is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discipline));

        _habitLookup = new HashSet<string>(StringComparer.Ordinal);
        var normalizedHabits = new List<string>();
        foreach (var habitId in habitIds)
        {
            if (string.IsNullOrWhiteSpace(habitId))
                throw new ArgumentException("Habit ids cannot be empty.", nameof(habitIds));
            var normalized = habitId.Trim();
            if (!_habitLookup.Add(normalized))
                throw new ArgumentException("Habit ids must be unique.", nameof(habitIds));
            normalizedHabits.Add(normalized);
        }

        Id = id;
        ProfessionId = professionId.Trim();
        Sociability = sociability;
        Discipline = discipline;
        HabitIds = Array.AsReadOnly(normalizedHabits.ToArray());
    }

    public ResidentId Id { get; }
    public string ProfessionId { get; }
    public int Sociability { get; }
    public int Discipline { get; }
    public IReadOnlyList<string> HabitIds { get; }

    public bool HasHabit(string habitId)
    {
        if (string.IsNullOrWhiteSpace(habitId))
            throw new ArgumentException("Habit id is required.", nameof(habitId));
        return _habitLookup.Contains(habitId.Trim());
    }
}

/// <summary>An abstract intent, not a game command. Social intent is not proof of conversation.</summary>
public sealed record AutonomyAction
{
    public AutonomyAction(
        AutonomyActionKind kind,
        string? professionId = null,
        ResidentId? targetResidentId = null)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        if (kind == AutonomyActionKind.Work)
        {
            if (string.IsNullOrWhiteSpace(professionId))
                throw new ArgumentException("Work actions require a profession id.", nameof(professionId));
            if (targetResidentId is not null)
                throw new ArgumentException("Work actions cannot have a social target.", nameof(targetResidentId));
            ProfessionId = professionId.Trim();
        }
        else
        {
            if (professionId is not null)
                throw new ArgumentException("Only work actions can carry a profession id.", nameof(professionId));
            ProfessionId = null;
        }

        if (kind != AutonomyActionKind.SeekSocialContact && targetResidentId is not null)
            throw new ArgumentException("Only social intents can carry a target resident.", nameof(targetResidentId));

        Kind = kind;
        TargetResidentId = targetResidentId;
    }

    public AutonomyActionKind Kind { get; }
    public string? ProfessionId { get; }
    public ResidentId? TargetResidentId { get; }
}

public sealed class ResidentAutonomyState
{
    public ResidentAutonomyState(
        ResidentAutonomyProfile profile,
        DailySchedule schedule,
        NeedState needs,
        AutonomyAction currentAction)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(needs);
        ArgumentNullException.ThrowIfNull(currentAction);

        if (currentAction.Kind == AutonomyActionKind.Work &&
            !string.Equals(currentAction.ProfessionId, profile.ProfessionId, StringComparison.Ordinal))
            throw new ArgumentException("A resident can only work under their configured profession.", nameof(currentAction));
        if (currentAction.TargetResidentId == profile.Id)
            throw new ArgumentException("A resident cannot seek social contact with themself.", nameof(currentAction));

        Profile = profile;
        Schedule = schedule;
        Needs = needs;
        CurrentAction = currentAction;
    }

    public ResidentAutonomyProfile Profile { get; }
    public DailySchedule Schedule { get; }
    public NeedState Needs { get; }
    public AutonomyAction CurrentAction { get; }

    internal ResidentAutonomyState With(NeedState needs, AutonomyAction action) =>
        new(Profile, Schedule, needs, action);
}

/// <summary>Immutable autonomy snapshot for exactly twelve unique residents.</summary>
public sealed class StationAutonomyState
{
    public const int ResidentCount = 12;

    public StationAutonomyState(IEnumerable<ResidentAutonomyState> residents, int minuteOfDay)
    {
        ArgumentNullException.ThrowIfNull(residents);
        if (minuteOfDay is < 0 or >= 1440)
            throw new ArgumentOutOfRangeException(nameof(minuteOfDay));

        var source = residents.ToArray();
        if (source.Length != ResidentCount)
            throw new ArgumentException("A station autonomy state requires exactly 12 residents.", nameof(residents));
        if (source.Any(resident => resident is null))
            throw new ArgumentException("Resident states cannot contain null.", nameof(residents));

        var ids = new HashSet<ResidentId>();
        foreach (var resident in source)
            if (!ids.Add(resident.Profile.Id))
                throw new ArgumentException("Resident ids must be unique.", nameof(residents));

        foreach (var resident in source)
        {
            var target = resident.CurrentAction.TargetResidentId;
            if (target is not null && !ids.Contains(target))
                throw new ArgumentException("A social intent target must belong to the station roster.", nameof(residents));
        }

        Residents = Array.AsReadOnly(source
            .OrderBy(resident => resident.Profile.Id.Value)
            .ToArray());
        MinuteOfDay = minuteOfDay;
    }

    public IReadOnlyList<ResidentAutonomyState> Residents { get; }
    public int MinuteOfDay { get; }
}

/// <summary>Configurable threshold and priority for one unmet need.</summary>
public sealed record NeedTrigger
{
    public NeedTrigger(
        NeedKind kind,
        int threshold,
        int priority,
        PersonalityAxis? personalityAxis = null,
        int thresholdOffsetPerPoint = 0)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (threshold is < 1 or > NeedState.Maximum)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority));
        if (personalityAxis is not null && !Enum.IsDefined(personalityAxis.Value))
            throw new ArgumentOutOfRangeException(nameof(personalityAxis));
        if (personalityAxis is null && thresholdOffsetPerPoint != 0)
            throw new ArgumentException("A personality offset requires a personality axis.", nameof(thresholdOffsetPerPoint));

        Kind = kind;
        Threshold = threshold;
        Priority = priority;
        PersonalityAxis = personalityAxis;
        ThresholdOffsetPerPoint = thresholdOffsetPerPoint;
    }

    public NeedKind Kind { get; }
    public int Threshold { get; }
    public int Priority { get; }
    public PersonalityAxis? PersonalityAxis { get; }
    public int ThresholdOffsetPerPoint { get; }

    public int GetEffectiveThreshold(ResidentAutonomyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var traitValue = PersonalityAxis switch
        {
            Protocol.Autonomy.PersonalityAxis.Sociability => profile.Sociability,
            Protocol.Autonomy.PersonalityAxis.Discipline => profile.Discipline,
            _ => 50
        };
        var adjusted = (long)Threshold - ((long)traitValue - 50L) * ThresholdOffsetPerPoint;
        return (int)Math.Clamp(adjusted, 1L, NeedState.Maximum);
    }
}

/// <summary>A habit may shape leisure only; it cannot bypass urgent needs or create unscheduled work.</summary>
public sealed record HabitActionPreference
{
    public HabitActionPreference(string habitId, AutonomyActionKind action, int priority)
    {
        if (string.IsNullOrWhiteSpace(habitId))
            throw new ArgumentException("Habit id is required.", nameof(habitId));
        if (action is not (AutonomyActionKind.SeekSocialContact or AutonomyActionKind.Decompress or AutonomyActionKind.Leisure))
            throw new ArgumentOutOfRangeException(nameof(action), "Habit preferences are limited to leisure, decompression or social intent.");
        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority));

        HabitId = habitId.Trim();
        Action = action;
        Priority = priority;
    }

    public string HabitId { get; }
    public AutonomyActionKind Action { get; }
    public int Priority { get; }
}

/// <summary>All thresholds, hourly deltas and social gates are explicit inputs; no game balance is embedded.</summary>
public sealed class AutonomyPolicy
{
    private readonly IReadOnlyDictionary<AutonomyActionKind, NeedDelta> _needEffects;
    private readonly IReadOnlyList<HabitActionPreference> _habitPreferences;

    public AutonomyPolicy(
        IEnumerable<NeedTrigger> needTriggers,
        IEnumerable<HabitActionPreference> habitPreferences,
        IEnumerable<KeyValuePair<AutonomyActionKind, NeedDelta>> needEffects,
        int minimumSocialTrust,
        int minimumSocialFamiliarity)
    {
        ArgumentNullException.ThrowIfNull(needTriggers);
        ArgumentNullException.ThrowIfNull(habitPreferences);
        ArgumentNullException.ThrowIfNull(needEffects);
        if (minimumSocialTrust is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(minimumSocialTrust));
        if (minimumSocialFamiliarity is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(minimumSocialFamiliarity));

        var triggers = needTriggers.ToArray();
        if (triggers.Length != Enum.GetValues<NeedKind>().Length ||
            triggers.Select(trigger => trigger.Kind).Distinct().Count() != triggers.Length)
            throw new ArgumentException("Exactly one trigger is required for each need kind.", nameof(needTriggers));
        if (triggers.Select(trigger => trigger.Priority).Distinct().Count() != triggers.Length)
            throw new ArgumentException("Need trigger priorities must be unique.", nameof(needTriggers));

        var preferences = habitPreferences.ToArray();
        if (preferences.Select(item => item.HabitId).Distinct(StringComparer.Ordinal).Count() != preferences.Length)
            throw new ArgumentException("Each habit id can have only one action preference.", nameof(habitPreferences));

        var effects = new Dictionary<AutonomyActionKind, NeedDelta>();
        foreach (var pair in needEffects)
        {
            if (!Enum.IsDefined(pair.Key))
                throw new ArgumentOutOfRangeException(nameof(needEffects), "Unknown autonomy action kind.");
            if (!effects.TryAdd(pair.Key, pair.Value))
                throw new ArgumentException("Need effects must not contain duplicate action kinds.", nameof(needEffects));
        }
        if (effects.Count != Enum.GetValues<AutonomyActionKind>().Length)
            throw new ArgumentException("A per-hour need effect is required for every action kind.", nameof(needEffects));

        NeedTriggers = Array.AsReadOnly(triggers.OrderBy(item => item.Priority).ToArray());
        _habitPreferences = Array.AsReadOnly(preferences
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.HabitId, StringComparer.Ordinal)
            .ToArray());
        _needEffects = new ReadOnlyDictionary<AutonomyActionKind, NeedDelta>(effects);
        MinimumSocialTrust = minimumSocialTrust;
        MinimumSocialFamiliarity = minimumSocialFamiliarity;
    }

    public IReadOnlyList<NeedTrigger> NeedTriggers { get; }
    public int MinimumSocialTrust { get; }
    public int MinimumSocialFamiliarity { get; }

    public NeedDelta GetNeedEffect(AutonomyActionKind action) =>
        _needEffects.TryGetValue(action, out var effect)
            ? effect
            : throw new ArgumentOutOfRangeException(nameof(action));

    public AutonomyActionKind? GetHabitAction(ResidentAutonomyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        foreach (var preference in _habitPreferences)
            if (profile.HasHabit(preference.HabitId))
                return preference.Action;
        return null;
    }
}

/// <summary>Selects an abstract action from needs, schedule, personality, habits and known relationships.</summary>
public static class AutonomyDecisionEngine
{
    public static AutonomyAction Decide(
        ResidentAutonomyState resident,
        int minuteOfDay,
        IReadOnlyList<ResidentId> stationResidents,
        RelationshipLedger relationships,
        AutonomyPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(resident);
        ArgumentNullException.ThrowIfNull(stationResidents);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(policy);
        if (minuteOfDay is < 0 or >= 1440)
            throw new ArgumentOutOfRangeException(nameof(minuteOfDay));

        foreach (var trigger in policy.NeedTriggers)
        {
            if (resident.Needs.Get(trigger.Kind) >= trigger.GetEffectiveThreshold(resident.Profile))
                return CreateNeedAction(trigger.Kind, resident, stationResidents, relationships, policy);
        }

        return resident.Schedule.GetActivityAt(minuteOfDay) switch
        {
            ScheduleActivity.Work => new AutonomyAction(AutonomyActionKind.Work, professionId: resident.Profile.ProfessionId),
            ScheduleActivity.Sleep => new AutonomyAction(AutonomyActionKind.Sleep),
            ScheduleActivity.Socialize => CreateSocialAction(resident, stationResidents, relationships, policy),
            ScheduleActivity.Leisure => CreateLeisureAction(resident, stationResidents, relationships, policy),
            _ => CreateFreeTimeAction(resident, stationResidents, relationships, policy)
        };
    }

    private static AutonomyAction CreateNeedAction(
        NeedKind need,
        ResidentAutonomyState resident,
        IReadOnlyList<ResidentId> stationResidents,
        RelationshipLedger relationships,
        AutonomyPolicy policy) =>
        need switch
        {
            NeedKind.Hunger => new AutonomyAction(AutonomyActionKind.Eat),
            NeedKind.Fatigue => new AutonomyAction(AutonomyActionKind.Sleep),
            NeedKind.Social => CreateSocialAction(resident, stationResidents, relationships, policy),
            NeedKind.Stress => new AutonomyAction(AutonomyActionKind.Decompress),
            _ => throw new ArgumentOutOfRangeException(nameof(need))
        };

    private static AutonomyAction CreateLeisureAction(
        ResidentAutonomyState resident,
        IReadOnlyList<ResidentId> stationResidents,
        RelationshipLedger relationships,
        AutonomyPolicy policy)
    {
        var habit = policy.GetHabitAction(resident.Profile);
        return habit switch
        {
            AutonomyActionKind.SeekSocialContact => CreateSocialAction(resident, stationResidents, relationships, policy),
            AutonomyActionKind.Decompress => new AutonomyAction(AutonomyActionKind.Decompress),
            _ => new AutonomyAction(AutonomyActionKind.Leisure)
        };
    }

    private static AutonomyAction CreateFreeTimeAction(
        ResidentAutonomyState resident,
        IReadOnlyList<ResidentId> stationResidents,
        RelationshipLedger relationships,
        AutonomyPolicy policy)
    {
        var habit = policy.GetHabitAction(resident.Profile);
        return habit switch
        {
            AutonomyActionKind.SeekSocialContact => CreateSocialAction(resident, stationResidents, relationships, policy),
            AutonomyActionKind.Decompress => new AutonomyAction(AutonomyActionKind.Decompress),
            AutonomyActionKind.Leisure => new AutonomyAction(AutonomyActionKind.Leisure),
            _ => new AutonomyAction(AutonomyActionKind.FreeTime)
        };
    }

    private static AutonomyAction CreateSocialAction(
        ResidentAutonomyState resident,
        IReadOnlyList<ResidentId> stationResidents,
        RelationshipLedger relationships,
        AutonomyPolicy policy)
    {
        var target = stationResidents
            .Where(candidate => candidate is not null && candidate != resident.Profile.Id)
            .Select(candidate => new { Id = candidate, Relationship = relationships.Get(resident.Profile.Id, candidate) })
            .Where(candidate =>
                candidate.Relationship.Trust >= policy.MinimumSocialTrust &&
                candidate.Relationship.Familiarity >= policy.MinimumSocialFamiliarity)
            .OrderByDescending(candidate => candidate.Relationship.Familiarity)
            .ThenByDescending(candidate => candidate.Relationship.Trust)
            .ThenBy(candidate => candidate.Id.Value)
            .Select(candidate => candidate.Id)
            .FirstOrDefault();

        return new AutonomyAction(AutonomyActionKind.SeekSocialContact, targetResidentId: target);
    }
}

/// <summary>Advances the previous action's effects, advances one hour, then selects the next action.</summary>
public static class StationAutonomySimulator
{
    public static StationAutonomyState AdvanceHour(
        StationAutonomyState state,
        AutonomyPolicy policy,
        RelationshipLedger relationships)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(relationships);

        var nextMinute = (state.MinuteOfDay + 60) % 1440;
        var residentIds = state.Residents.Select(item => item.Profile.Id).ToArray();
        var nextResidents = new List<ResidentAutonomyState>(StationAutonomyState.ResidentCount);

        foreach (var resident in state.Residents)
        {
            var updatedNeeds = resident.Needs.Apply(policy.GetNeedEffect(resident.CurrentAction.Kind));
            var updatedResident = resident.With(updatedNeeds, resident.CurrentAction);
            var nextAction = AutonomyDecisionEngine.Decide(
                updatedResident, nextMinute, residentIds, relationships, policy);
            nextResidents.Add(updatedResident.With(updatedNeeds, nextAction));
        }

        return new StationAutonomyState(nextResidents, nextMinute);
    }
}
