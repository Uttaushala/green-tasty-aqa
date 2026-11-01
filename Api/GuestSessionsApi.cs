using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>What an invited guest does: join a group, fill their cart and confirm it. The guest's session token
/// (from <see cref="CreateSessionAsync"/>) is the bearer token of every call after joining.</summary>
public sealed class GuestSessionsApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> CreateSessionAsync(CreateGuestSessionRequest request) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(),
        });

    public Task<IAPIResponse> AddDishAsync(string guestSessionId, GuestDishRequest request, string sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(sessionToken),
        });

    public Task<IAPIResponse> ConfirmCartAsync(string guestSessionId, string sessionToken) =>
        Api.PostAsync(Url($"/guest/sessions/{guestSessionId}/submit"), new APIRequestContextOptions
        {
            Headers = Headers(sessionToken),
        });
}
