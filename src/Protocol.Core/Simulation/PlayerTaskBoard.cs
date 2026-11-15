namespace Protocol.Core.Simulation;

public enum PlayerTaskStatus
{
    Active,
    Completed,
    Cancelled
}

public enum PlayerTaskPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// An objective assigned to the human-controlled resident. The board belongs
/// to one player identity and is the source for the in-game personal task HUD.
/// </summary>
public sealed record PlayerTask
{
    public PlayerTask(
        Guid id,
        string title,
        string description,
        string source,
        PlayerTaskPriority priority,
        long createdAtTick)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Task id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Task source is required.", nameof(source));
        if (createdAtTick < 0)
            throw new ArgumentOutOfRangeException(nameof(createdAtTick));

        Id = id;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Source = source.Trim();
        Priority = priority;
        CreatedAtTick = createdAtTick;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public string Source { get; }
    public PlayerTaskPriority Priority { get; }
    public long CreatedAtTick { get; }
}

public sealed record PlayerTaskHudItem(
    Guid Id,
    string Title,
    string Description,
    string Source,
    PlayerTaskPriority Priority,
    PlayerTaskStatus Status);

public sealed record PlayerTaskHudSnapshot(
    Guid PlayerResidentId,
    string Header,
    IReadOnlyList<PlayerTaskHudItem> Tasks,
    int ActiveCount);

/// <summary>
/// Stores tasks for exactly one player. NPC work orders and other residents'
/// objectives cannot leak into this board unless explicitly assigned here.
/// </summary>
public sealed class PlayerTaskBoard
{
    private readonly Dictionary<Guid, (PlayerTask Task, PlayerTaskStatus Status)> _tasks = new();

    public PlayerTaskBoard(Guid playerResidentId)
    {
        if (playerResidentId == Guid.Empty)
            throw new ArgumentException("Player resident id cannot be empty.", nameof(playerResidentId));
        PlayerResidentId = playerResidentId;
    }

    public Guid PlayerResidentId { get; }

    public void Assign(PlayerTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (_tasks.ContainsKey(task.Id))
            throw new InvalidOperationException("A task with this id is already assigned.");
        _tasks.Add(task.Id, (task, PlayerTaskStatus.Active));
    }

    public bool Complete(Guid taskId) => SetStatus(taskId, PlayerTaskStatus.Completed);

    public bool Cancel(Guid taskId) => SetStatus(taskId, PlayerTaskStatus.Cancelled);

    public PlayerTaskHudSnapshot CreateHudSnapshot(bool includeFinished = false)
    {
        var items = _tasks.Values
            .Where(item => includeFinished || item.Status == PlayerTaskStatus.Active)
            .OrderByDescending(item => item.Task.Priority)
            .ThenBy(item => item.Task.CreatedAtTick)
            .Select(item => new PlayerTaskHudItem(
                item.Task.Id,
                item.Task.Title,
                item.Task.Description,
                item.Task.Source,
                item.Task.Priority,
                item.Status))
            .ToArray();

        return new PlayerTaskHudSnapshot(
            PlayerResidentId,
            "МОИ ЗАДАНИЯ",
            items,
            _tasks.Values.Count(item => item.Status == PlayerTaskStatus.Active));
    }

    private bool SetStatus(Guid taskId, PlayerTaskStatus status)
    {
        if (!_tasks.TryGetValue(taskId, out var current) ||
            current.Status != PlayerTaskStatus.Active)
            return false;

        _tasks[taskId] = (current.Task, status);
        return true;
    }
}
