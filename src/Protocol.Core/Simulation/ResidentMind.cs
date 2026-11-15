using Protocol.Core.Domain;
using Protocol.Core.Social;

namespace Protocol.Core.Simulation;

/// <summary>
/// Runtime state belonging to one resident. Knowledge stays personal unless
/// an explicit observation or conversation transfers it.
/// </summary>
public sealed class ResidentMind
{
    public ResidentMind(Resident resident, ResidentNeeds? needs = null, MemoryJournal? memories = null)
    {
        Resident = resident ?? throw new ArgumentNullException(nameof(resident));
        Needs = needs ?? new ResidentNeeds();
        Memories = memories ?? new MemoryJournal();
    }

    public Resident Resident { get; }
    public ResidentNeeds Needs { get; }
    public MemoryJournal Memories { get; }
    public ResidentAction? CurrentAction { get; private set; }

    public void SetAction(ResidentAction action) => CurrentAction = action;
}
