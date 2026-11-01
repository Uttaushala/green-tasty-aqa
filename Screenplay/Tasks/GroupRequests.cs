using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Asks for the list of participants of the owner's group, as <paramref name="who"/>. A group id can be
/// given to ask for a group that does not exist. The outcome is the scenario's last response.</summary>
public sealed class RequestTheParticipantList(Requester who, string? groupId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var token = await Credentials.BearerTokenOfAsync(actor, who);
        var group = groupId ?? actor.Recall<GroupBooking>("owner").GroupId;
        await actor.TheLastResponse().CaptureAsync(await new GroupOrdersApi(actor.UsingTheApi()).GetParticipantsAsync(group, token));
    }
}

/// <summary>Sends the group's shared pre-order to the restaurant, as <paramref name="who"/>. A group or reservation
/// id can be given to send for something that does not exist. The outcome is the scenario's last response.</summary>
public sealed class ConfirmTheGroupPreOrder(Requester who, string? groupId = null, string? reservationId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var token = await Credentials.BearerTokenOfAsync(actor, who);
        var booking = actor.Recall<GroupBooking>("owner");
        await actor.TheLastResponse().CaptureAsync(await new GroupOrdersApi(actor.UsingTheApi())
            .SubmitAsync(groupId ?? booking.GroupId, reservationId ?? booking.Reservation.Id, token));
    }
}

/// <summary>Removes a guest from the owner's group, as <paramref name="who"/>. The guest is named, or an id is
/// given for a guest that does not exist. The outcome is the scenario's last response.</summary>
public sealed class RemoveGuestFromTheGroup(Requester who, string? guestName = null, string? guestSessionId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var token = await Credentials.BearerTokenOfAsync(actor, who);
        var group = actor.Recall<GroupBooking>("owner").GroupId;
        var sessionId = guestSessionId
            ?? actor.Recall<GroupGuest>(guestName ?? throw new InvalidOperationException("Name the guest to remove.")).SessionId;

        await actor.TheLastResponse().CaptureAsync(await new GroupOrdersApi(actor.UsingTheApi()).RemoveGuestAsync(group, sessionId, token));
    }
}
