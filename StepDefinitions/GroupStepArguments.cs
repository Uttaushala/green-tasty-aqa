using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>The words that say who is making a request in the group pre-order features.</summary>
[Binding]
public static class GroupStepArguments
{
    [StepArgumentTransformation(Name = "requester", Regex = "(the owner|another customer|a guest of the group|the waiter|an anonymous visitor)")]
    public static Requester ToRequester(string value) => value switch
    {
        "the owner" => Requester.Owner,
        "another customer" => Requester.AnotherCustomer,
        "a guest of the group" => Requester.GroupGuest,
        "the waiter" => Requester.Waiter,
        "an anonymous visitor" => Requester.Anonymous,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
