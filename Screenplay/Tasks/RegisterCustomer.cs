using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Registers a brand-new customer (unique email, prefixed name) and signs them in through the API.
/// The customer is remembered under <paramref name="role"/>. Customers cannot be deleted afterwards; they are
/// recognisable by the sample prefix and a fresh email on every run.</summary>
public sealed class RegisterCustomer(string role) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var details = SampleDataFactory.RegisterRequest();
        var auth = new AuthApi(actor.UsingTheApi());

        var registration = await auth.RegisterAsync(details);
        if (!registration.Ok)
            throw new InvalidOperationException($"Registering the {role} failed with {registration.Status}: {await registration.ReadErrorMessageAsync()}");

        var signIn = await auth.SignInAsync(new SignInRequest(details.Email, details.Password));
        if (!signIn.Ok)
            throw new InvalidOperationException($"Signing in the {role} failed with {signIn.Status}: {await signIn.ReadErrorMessageAsync()}");

        var token = (await signIn.ReadAsAsync<SignInResponse>()).IdToken;
        actor.Remember(role, new CustomerSession(role, details, token));
    }
}
