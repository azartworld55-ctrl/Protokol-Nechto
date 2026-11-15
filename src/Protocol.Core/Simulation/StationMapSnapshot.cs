namespace Protocol.Core.Simulation;

/// <summary>
/// Integer tile coordinate independent of any game engine.
/// </summary>
public readonly record struct GridPosition(int X, int Z)
{
    public int ManhattanDistanceTo(GridPosition other) =>
        Math.Abs(X - other.X) + Math.Abs(Z - other.Z);

    public int SquaredDistanceTo(GridPosition other)
    {
        var dx = X - other.X;
        var dz = Z - other.Z;
        return dx * dx + dz * dz;
    }
}

/// <summary>
/// Minimal map facts needed by perception. Closed doors and solid walls
/// block vision; open doors should be supplied as non-blocking cells.
/// </summary>
public readonly record struct MapCell(bool BlocksMovement, bool BlocksVision)
{
    public static MapCell Floor { get; } = new(false, false);
    public static MapCell Wall { get; } = new(true, true);
    public static MapCell ClosedDoor { get; } = new(true, true);
    public static MapCell OpenDoor { get; } = new(false, false);
}

/// <summary>
/// Immutable map snapshot supplied by the game adapter for one simulation tick.
/// It contains positions, not hidden resident identities or their knowledge.
/// </summary>
public sealed class StationMapSnapshot
{
    private readonly MapCell[] _cells;
    private readonly IReadOnlyDictionary<Guid, GridPosition> _residentPositions;

    public StationMapSnapshot(
        int width,
        int height,
        IEnumerable<MapCell> cells,
        IReadOnlyDictionary<Guid, GridPosition> residentPositions)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(residentPositions);

        var materialized = cells.ToArray();
        if (materialized.Length != checked(width * height))
            throw new ArgumentException("Map cell count must equal width multiplied by height.", nameof(cells));

        var positions = new Dictionary<Guid, GridPosition>();
        foreach (var pair in residentPositions)
        {
            if (pair.Key == Guid.Empty)
                throw new ArgumentException("Resident position keys cannot be empty.", nameof(residentPositions));
            if (!IsInside(pair.Value, width, height))
                throw new ArgumentException("Resident position lies outside the map.", nameof(residentPositions));
            var cell = materialized[pair.Value.Z * width + pair.Value.X];
            if (cell.BlocksMovement)
                throw new ArgumentException("A resident cannot occupy a movement-blocking cell.", nameof(residentPositions));
            positions.Add(pair.Key, pair.Value);
        }

        Width = width;
        Height = height;
        _cells = materialized;
        _residentPositions = positions;
    }

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyDictionary<Guid, GridPosition> ResidentPositions => _residentPositions;

    public bool Contains(GridPosition position) => IsInside(position, Width, Height);

    public MapCell GetCell(GridPosition position)
    {
        if (!Contains(position))
            throw new ArgumentOutOfRangeException(nameof(position));
        return _cells[position.Z * Width + position.X];
    }

    public bool HasLineOfSight(GridPosition origin, GridPosition target)
    {
        if (!Contains(origin) || !Contains(target))
            return false;
        if (origin == target)
            return true;

        // Bresenham traversal. The target tile itself may be opaque (e.g. a
        // wall surface is visible); only intervening cells occlude the target.
        var x = origin.X;
        var z = origin.Z;
        var dx = Math.Abs(target.X - origin.X);
        var dz = Math.Abs(target.Z - origin.Z);
        var stepX = origin.X < target.X ? 1 : -1;
        var stepZ = origin.Z < target.Z ? 1 : -1;
        var error = dx - dz;

        while (x != target.X || z != target.Z)
        {
            var doubledError = 2 * error;
            var previousX = x;
            var previousZ = z;
            var stepInX = doubledError > -dz;
            var stepInZ = doubledError < dx;

            if (stepInX)
            {
                error -= dz;
                x += stepX;
            }
            if (stepInZ)
            {
                error += dx;
                z += stepZ;
            }

            // Do not allow diagonal sight through the corner between two
            // opaque cells (e.g. two walls meeting at a corner).
            if (stepInX && stepInZ)
            {
                var sideX = _cells[previousZ * Width + x];
                var sideZ = _cells[z * Width + previousX];
                if (sideX.BlocksVision || sideZ.BlocksVision)
                    return false;
            }

            if (x == target.X && z == target.Z)
                return true;
            if (_cells[z * Width + x].BlocksVision)
                return false;
        }

        return true;
    }

    private static bool IsInside(GridPosition position, int width, int height) =>
        position.X >= 0 && position.X < width &&
        position.Z >= 0 && position.Z < height;
}
