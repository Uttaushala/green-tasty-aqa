using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The customer who booked the table places their own pre-order (dishes by their short labels) for the
/// reservation. The service sends a group pre-order only into an order that already exists, so this is what the
/// shared order is built on. The order goes away with the reservation.</summary>
public sealed class PlaceThePreOrder(string role, IReadOnlyList<CartLine> lines) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var customer = actor.Recall<CustomerSession>(role);
        var reservationId = actor.Recall<GroupBooking>(role).Reservation.Id;
        var items = lines.Select(line => new OrderItemRequest(actor.Recall<OfferedDish>(line.Dish).Dish.Id, line.Quantity)).ToList();

        var response = actor.TheLastResponse();
        await response.CaptureAsync(await new OrdersApi(actor.UsingTheApi())
            .CreateOrderAsync(new CreateOrderRequest(reservationId, items), customer.IdToken));
        if (!response.Succeeded)
            throw new InvalidOperationException($"Placing the pre-order failed with {response.Status}: {response.Body}");
    }
}
