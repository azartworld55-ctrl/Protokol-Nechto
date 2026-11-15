using Protocol.Core.Domain;

namespace Protocol.Core.Simulation;

public enum WorkTask
{
    General,
    Research,
    MedicalCare,
    Engineering,
    Cooking,
    Maintenance,
    Communications
}

/// <summary>
/// Separates a resident's job from the concrete game-world workstation.
/// This profile is safe to use before map and RimWorld APIs are available.
/// </summary>
public sealed class ResidentWorkProfile
{
    public ResidentWorkProfile(WorkTask task, WorkSchedule schedule)
    {
        Task = task;
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
    }

    public WorkTask Task { get; }
    public WorkSchedule Schedule { get; }

    public static ResidentWorkProfile FromProfession(
        string profession,
        WorkSchedule? schedule = null)
    {
        if (string.IsNullOrWhiteSpace(profession))
            throw new ArgumentException("Profession is required.", nameof(profession));

        var normalized = profession.Trim().ToLowerInvariant();
        var task = normalized switch
        {
            var value when value.Contains("учён") || value.Contains("учен") ||
                          value.Contains("scientist") || value.Contains("research") => WorkTask.Research,
            var value when value.Contains("врач") || value.Contains("мед") ||
                          value.Contains("doctor") || value.Contains("medic") => WorkTask.MedicalCare,
            var value when value.Contains("инжен") || value.Contains("engineer") => WorkTask.Engineering,
            var value when value.Contains("повар") || value.Contains("cook") ||
                          value.Contains("chef") => WorkTask.Cooking,
            var value when value.Contains("техник") || value.Contains("механ") ||
                          value.Contains("technician") || value.Contains("mechanic") => WorkTask.Maintenance,
            var value when value.Contains("радист") || value.Contains("связ") ||
                          value.Contains("radio") || value.Contains("communicat") => WorkTask.Communications,
            _ => WorkTask.General
        };

        return new ResidentWorkProfile(task, schedule ?? WorkSchedule.StandardDay());
    }
}

public sealed record ResidentWorkOrder(
    Guid ResidentId,
    string Profession,
    WorkTask Task,
    int HourOfDay);

/// <summary>
/// Creates abstract work orders only for residents whose shifts are active.
/// It does not claim that a workstation exists or that a task was executed.
/// </summary>
public sealed class ResidentWorkPlanner
{
    public IReadOnlyList<ResidentWorkOrder> Plan(
        IEnumerable<ResidentMind> minds,
        int hourOfDay)
    {
        ArgumentNullException.ThrowIfNull(minds);
        if (hourOfDay is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hourOfDay));

        return minds
            .Where(mind => mind.WorkProfile.Schedule.IsWorkingAt(hourOfDay))
            .OrderBy(mind => mind.Resident.Id)
            .Select(mind => new ResidentWorkOrder(
                mind.Resident.Id,
                mind.Resident.Profession,
                mind.WorkProfile.Task,
                hourOfDay))
            .ToArray();
    }
}
