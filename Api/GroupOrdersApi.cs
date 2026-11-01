using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>The reservation owner's side of a group pre-order: finding the group, seeing who joined,
/// removing a guest and sending the shared order. Every call takes the caller's bearer token (or none, to
/// check that anonymous callers are turned away).</summary>
public sealed class GroupOrdersApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> GetInviteTokenAsync(string reservationId, string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"), WithToken(bearerToken));

    public Task<IAPIResponse> GetInviteAsync(string inviteToken) =>
        GetAsync(Url($"/guest/invites/{inviteToken}"), WithToken(null));

    public Task<IAPIResponse> GetGroupReservationAsync(string reservationId, string? bearerToken = null) =>
        GetAsync(Url($"/reservations/{reservationId}/group-reservation"), WithToken(bearerToken));

    public Task<IAPIResponse> GetParticipantsAsync(string groupReservationId, string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/sessions"), WithToken(bearerToken));

    public Task<IAPIResponse> GetStatusAsync(string groupReservationId, string? bearerToken = null) =>
        GetAsync(Url($"/guest/groups/{groupReservationId}/status"), WithToken(bearerToken));

    public Task<IAPIResponse> RemoveGuestAsync(string groupReservationId, string guestSessionId, string? bearerToken = null) =>
        DeleteAsync(Url($"/guest/groups/{groupReservationId}/sessions/{guestSessionId}"), WithToken(bearerToken));

    public Task<IAPIResponse> SubmitAsync(string groupReservationId, string reservationId, string? bearerToken = null) =>
        Api.PostAsync(Url($"/group-reservations/{groupReservationId}/submit"), new APIRequestContextOptions
        {
            DataString = Serialize(new SubmitGroupRequest(reservationId)),
            Headers = Headers(bearerToken),
        });

    private static APIRequestContextOptions WithToken(string? bearerToken) => new() { Headers = Headers(bearerToken) };
}
