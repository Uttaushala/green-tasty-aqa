using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The participant list the last request answered with (an earlier task asked for it).</summary>
public sealed class TheParticipantList : IQuestion<GroupSessionsResponse>
{
    public Task<GroupSessionsResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GroupSessionsResponse>());
}

/// <summary>Where the owner's group pre-order stands right now ("Open", "Submitted" or "Closed"). Asks the
/// service itself and leaves the scenario's last response untouched.</summary>
public sealed class TheStateOfTheGroupPreOrder : IQuestion<GroupStatusResponse>
{
    public async Task<GroupStatusResponse> AnsweredByAsync(Actor actor)
    {
        var owner = actor.Recall<CustomerSession>("owner");
        var group = actor.Recall<GroupBooking>("owner").GroupId;
        var response = await new GroupOrdersApi(actor.UsingTheApi()).GetStatusAsync(group, owner.IdToken);
        if (!response.Ok)
            throw new InvalidOperationException($"Asking for the group status failed with {response.Status}: {await response.ReadErrorMessageAsync()}");

        return await response.ReadAsAsync<GroupStatusResponse>();
    }
}

/// <summary>The restaurant's order for the owner's reservation as <paramref name="who"/> sees it, or null when
/// there is no order. Asks the service itself and leaves the scenario's last response untouched.</summary>
public sealed class TheOrderOfTheReservation(Requester who) : IQuestion<GetOrderResponse?>
{
    public async Task<GetOrderResponse?> AnsweredByAsync(Actor actor)
    {
        var token = await Credentials.BearerTokenOfAsync(actor, who);
        var reservationId = actor.Recall<GroupBooking>("owner").Reservation.Id;
        var response = await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(reservationId, token);

        if (response.Status == 404)
            return null;

        if (!response.Ok)
            throw new InvalidOperationException($"Asking for the order failed with {response.Status}: {await response.ReadErrorMessageAsync()}");

        return await response.ReadAsAsync<GetOrderResponse>();
    }
}
