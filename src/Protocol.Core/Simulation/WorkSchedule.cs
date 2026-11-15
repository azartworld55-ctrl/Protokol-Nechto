namespace Protocol.Core.Simulation;

/// <summary>
/// Local station-time work intervals. An interval may cross midnight,
/// for example 22:00–06:00. Equal endpoints are rejected rather than
/// interpreted ambiguously as either zero or twenty-four hours.
/// </summary>
public sealed class WorkSchedule
{
    private readonly WorkShift[] _shifts;

    public WorkSchedule(IEnumerable<WorkShift> shifts)
    {
        ArgumentNullException.ThrowIfNull(shifts);
        _shifts = shifts.ToArray();
        if (_shifts.Length == 0)
            throw new ArgumentException("At least one work shift is required.", nameof(shifts));
    }

    public IReadOnlyList<WorkShift> Shifts => Array.AsReadOnly(_shifts);

    public bool IsWorkingAt(int hourOfDay)
    {
        if (hourOfDay is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hourOfDay));

        return _shifts.Any(shift => shift.Contains(hourOfDay));
    }

    public static WorkSchedule StandardDay() => new(new[] { new WorkShift(8, 16) });
}

public readonly record struct WorkShift
{
    public WorkShift(int startHour, int endHour)
    {
        if (startHour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(startHour));
        if (endHour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(endHour));
        if (startHour == endHour)
            throw new ArgumentException("A work shift must have different start and end hours.");

        StartHour = startHour;
        EndHour = endHour;
    }

    public int StartHour { get; }
    public int EndHour { get; }

    public bool Contains(int hourOfDay) =>
        StartHour < EndHour
            ? hourOfDay >= StartHour && hourOfDay < EndHour
            : hourOfDay >= StartHour || hourOfDay < EndHour;
}
