using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest joins the group of the booking made by <paramref name="groupOf"/> (the owner, unless said
/// otherwise) and is remembered under the short name. The guest disappears with the reservation.</summary>
public sealed class JoinTheGroup(string name, string groupOf = "owner") : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var booking = actor.Recall<GroupBooking>(groupOf);
        var displayName = SampleDataFactory.Prefix + name;

        var response = actor.TheLastResponse();
        await response.CaptureAsync(await new GuestSessionsApi(actor.UsingTheApi())
            .CreateSessionAsync(new CreateGuestSessionRequest(booking.GroupId, displayName)));
        if (!response.Succeeded)
            throw new InvalidOperationException($"{name} could not join the group: {response.Status}: {response.Body}");

        var session = response.Json<CreateGuestSessionResponse>();
        actor.Remember(name, new GroupGuest(name, displayName, session.SessionId, session.SessionToken));
    }
}

/// <summary>A guest puts dishes (by their short labels) in their cart.</summary>
public sealed class AddToTheCart(string guestName, IReadOnlyList<CartLine> lines) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GroupGuest>(guestName);
        var api = new GuestSessionsApi(actor.UsingTheApi());

        foreach (var line in lines)
        {
            var dish = actor.Recall<OfferedDish>(line.Dish).Dish;
            var response = actor.TheLastResponse();
            await response.CaptureAsync(await api.AddDishAsync(
                guest.SessionId, new GuestDishRequest(dish.Id, line.Quantity), guest.SessionToken));
            if (!response.Succeeded)
                throw new InvalidOperationException($"{guestName} could not add '{line.Dish}': {response.Status}: {response.Body}");
        }
    }
}

/// <summary>A guest confirms their cart, which is what makes it count for the shared order.</summary>
public sealed class ConfirmTheCart(string guestName, bool mustSucceed = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GroupGuest>(guestName);
        var response = actor.TheLastResponse();
        await response.CaptureAsync(await new GuestSessionsApi(actor.UsingTheApi())
            .ConfirmCartAsync(guest.SessionId, guest.SessionToken));
        if (mustSucceed && !response.Succeeded)
            throw new InvalidOperationException($"{guestName} could not confirm the cart: {response.Status}: {response.Body}");
    }
}
