namespace Protocol.Core.Simulation;

/// <summary>
/// Derives personal visibility and physically plausible social contacts from
/// one map snapshot. NPC contacts are computed independently of player vision.
/// </summary>
public sealed class StationPerceptionService
{
    public IReadOnlySet<GridPosition> GetVisibleCells(
        StationMapSnapshot map,
        Guid observerId,
        int sightRadius)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (observerId == Guid.Empty)
            throw new ArgumentException("Observer id cannot be empty.", nameof(observerId));
        if (sightRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(sightRadius));
        if (!map.ResidentPositions.TryGetValue(observerId, out var origin))
            throw new ArgumentException("Observer is not present in the map snapshot.", nameof(observerId));

        var radiusSquared = sightRadius * sightRadius;
        var visible = new HashSet<GridPosition>();
        for (var z = Math.Max(0, origin.Z - sightRadius);
             z <= Math.Min(map.Height - 1, origin.Z + sightRadius);
             z++)
        {
            for (var x = Math.Max(0, origin.X - sightRadius);
                 x <= Math.Min(map.Width - 1, origin.X + sightRadius);
                 x++)
            {
                var target = new GridPosition(x, z);
                if (origin.SquaredDistanceTo(target) <= radiusSquared &&
                    map.HasLineOfSight(origin, target))
                    visible.Add(target);
            }
        }

        return visible;
    }

    public IReadOnlySet<Guid> GetVisibleResidents(
        StationMapSnapshot map,
        Guid observerId,
        int sightRadius)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (!map.ResidentPositions.ContainsKey(observerId))
            throw new ArgumentException("Observer is not present in the map snapshot.", nameof(observerId));

        var visibleCells = GetVisibleCells(map, observerId, sightRadius);
        return map.ResidentPositions
            .Where(pair => pair.Key != observerId && visibleCells.Contains(pair.Value))
            .Select(pair => pair.Key)
            .ToHashSet();
    }

    public IReadOnlySet<Guid> GetResidentsWhoCanSee(
        StationMapSnapshot map,
        GridPosition eventPosition,
        int sightRadius)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (!map.Contains(eventPosition))
            throw new ArgumentOutOfRangeException(nameof(eventPosition));
        if (sightRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(sightRadius));

        var radiusSquared = sightRadius * sightRadius;
        return map.ResidentPositions
            .Where(pair => pair.Value.SquaredDistanceTo(eventPosition) <= radiusSquared)
            .Where(pair => map.HasLineOfSight(pair.Value, eventPosition))
            .Select(pair => pair.Key)
            .ToHashSet();
    }

    public IReadOnlyList<SocialContact> DetectSocialContacts(
        StationMapSnapshot map,
        long tick,
        int conversationRadius = 3)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (tick < 0)
            throw new ArgumentOutOfRangeException(nameof(tick));
        if (conversationRadius < 1)
            throw new ArgumentOutOfRangeException(nameof(conversationRadius));

        var residents = map.ResidentPositions
            .OrderBy(pair => pair.Key)
            .ToArray();
        var maxDistanceSquared = conversationRadius * conversationRadius;
        var contacts = new List<SocialContact>();

        for (var i = 0; i < residents.Length; i++)
        {
            for (var j = i + 1; j < residents.Length; j++)
            {
                var first = residents[i];
                var second = residents[j];
                if (first.Value.SquaredDistanceTo(second.Value) > maxDistanceSquared)
                    continue;
                if (!map.HasLineOfSight(first.Value, second.Value))
                    continue;

                contacts.Add(new SocialContact(first.Key, second.Key, tick));
            }
        }

        return contacts;
    }
}
