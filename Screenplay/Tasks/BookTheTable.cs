using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A registered customer books the scenario's most recent table for <paramref name="guests"/> guests,
/// starting <paramref name="startsInMinutes"/> minutes from now (the time is worked out when the scenario runs,
/// never written down). The booking brings a group pre-order with it; both are remembered under the customer's
/// role, and the reservation is cancelled again when the scenario ends.</summary>
public sealed class BookTheTable(string role, int guests, int startsInMinutes) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var customer = actor.Recall<CustomerSession>(role);
        var table = actor.Recall<TableResponse>();
        var api = actor.UsingTheApi();
        var reservations = new ReservationsApi(api);

        var start = DateTime.UtcNow.AddMinutes(startsInMinutes);
        var response = actor.TheLastResponse();
        await response.CaptureAsync(await reservations.CreateReservationAsync(
            SampleDataFactory.CreateReservationRequest(table.Id, start, guests), customer.IdToken));
        if (!response.Succeeded)
            throw new InvalidOperationException($"Booking the table for the {role} failed with {response.Status}: {response.Body}");

        var reservation = response.Json<ReservationResponse>();
        actor.Recall<CleanupScope>().TrackDelete(
            $"reservation:{reservation.Id}",
            () => new ReservationsApi(api).CancelReservationAsync(reservation.Id, customer.IdToken));

        var invite = await FindTheGroupAsync(new GroupOrdersApi(api), reservation.Id, customer.IdToken);
        actor.Remember(role, new GroupBooking(reservation, invite));
    }

    /// <summary>The group is found the way the storefront finds it: through the invite link, and, once the
    /// invite is no longer offered, through the owner's own view of the group.</summary>
    private static async Task<GuestInviteResponse> FindTheGroupAsync(GroupOrdersApi groups, string reservationId, string token)
    {
        var tokenResponse = await groups.GetInviteTokenAsync(reservationId, token);
        if (tokenResponse.Ok)
        {
            var invite = await groups.GetInviteAsync((await tokenResponse.ReadAsAsync<InviteTokenResponse>()).Token);
            if (invite.Ok)
                return await invite.ReadAsAsync<GuestInviteResponse>();
        }

        var group = await groups.GetGroupReservationAsync(reservationId, token);
        if (!group.Ok)
            throw new InvalidOperationException($"Finding the group of the reservation failed with {group.Status}: {await group.ReadErrorMessageAsync()}");

        return await group.ReadAsAsync<GuestInviteResponse>();
    }
}
