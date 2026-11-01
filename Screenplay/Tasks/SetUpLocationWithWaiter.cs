using System.Text.Json;
using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Sets up a sample location together with its own waiter. The backend hands every reservation to a waiter
/// of the table's location, and a waiter belongs to exactly one location, so a scenario that books a table needs a
/// waiter that is not shared with any other scenario. The waiter is a freshly registered customer who is added to
/// the waiters of the new location (the service takes no credentials for that) and signs in again to get a token that
/// carries the waiter role. That token becomes the scenario's waiter session, so every later task that needs a waiter
/// (adding tables and dishes, removing them again, reading an order) uses it. The waiter and the location are removed
/// when the scenario ends.</summary>
public sealed class SetUpLocationWithWaiter : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = actor.UsingTheApi();
        var locations = new LocationsApi(api);
        var response = actor.TheLastResponse();

        await response.CaptureAsync(await locations.CreateLocationAsync(SampleDataFactory.CreateLocationRequest()));
        if (!response.Succeeded)
            throw new InvalidOperationException($"Setting up the location failed with {response.Status}: {response.Body}");

        var location = response.Json<LocationResponse>();
        actor.Remember(location);

        await actor.AttemptsToAsync(new RegisterCustomer("waiter"));
        var waiter = actor.Recall<CustomerSession>("waiter");

        var added = await api.PostAsync(Url("/waiters"), WaiterRequest(waiter.Details.Email, location.Id));
        if (!added.Ok)
            throw new InvalidOperationException($"Adding the waiter to the location failed with {added.Status}: {await added.ReadErrorMessageAsync()}");

        var signIn = await new AuthApi(api).SignInAsync(new SignInRequest(waiter.Details.Email, waiter.Details.Password));
        if (!signIn.Ok)
            throw new InvalidOperationException($"Signing in the waiter failed with {signIn.Status}: {await signIn.ReadErrorMessageAsync()}");

        var token = (await signIn.ReadAsAsync<SignInResponse>()).IdToken;
        actor.Remember(new WaiterSession(token));

        // Cleanup runs in reverse order: whatever the scenario adds later (reservations, dishes, tables) goes first,
        // then the location, and the waiter, registered first, goes last.
        var cleanup = actor.Recall<CleanupScope>();
        cleanup.TrackDelete($"waiter:{waiter.Details.Email}", () => api.DeleteAsync(Url("/waiters"), WaiterRequest(waiter.Details.Email, location.Id)));
        cleanup.TrackDelete($"location:{location.Id}", () => locations.DeleteLocationAsync(location.Id, force: true, bearerToken: token));
    }

    private static string Url(string path) => Settings.Current.ApiBaseUrl.TrimEnd('/') + path;

    private static APIRequestContextOptions WaiterRequest(string email, string locationId) => new()
    {
        DataString = JsonSerializer.Serialize(new { email, locationId }),
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
    };
}
