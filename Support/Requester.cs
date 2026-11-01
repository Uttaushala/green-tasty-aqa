using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Support;

/// <summary>Who is making a request in a scenario about a group pre-order.</summary>
public enum Requester
{
    Owner,
    AnotherCustomer,
    GroupGuest,
    Waiter,
    Anonymous,
}

public static class Credentials
{
    /// <summary>The bearer token the requester would send, or null for someone who is not signed in at all.</summary>
    public static async Task<string?> BearerTokenOfAsync(Actor actor, Requester who) => who switch
    {
        Requester.Owner => actor.Recall<CustomerSession>("owner").IdToken,
        Requester.AnotherCustomer => actor.Recall<CustomerSession>("another customer").IdToken,
        Requester.GroupGuest => actor.RecallAll<GroupGuest>().Count > 0
            ? actor.RecallAll<GroupGuest>()[0].SessionToken
            : throw new InvalidOperationException("This scenario has no guest who joined the group yet."),
        Requester.Waiter => (await actor.AsksForAsync(new TheWaiterSession())).IdToken,
        Requester.Anonymous => null,
        _ => throw new ArgumentOutOfRangeException(nameof(who), who, null),
    };
}
