using Restaurant_Aqa_Framework.Screenplay;

namespace Restaurant_Aqa_Framework.Support;

/// <summary>Feature files call a dish "soup"; the application knows it by the unique prefixed name the scenario
/// gave it. This turns the first into the second so a scenario can compare what it set up with what it sees.</summary>
public static class DishNames
{
    public static IReadOnlyList<CartLine> ByName(Actor actor, IEnumerable<CartLine> byLabel) =>
        byLabel.Select(line => new CartLine(actor.Recall<OfferedDish>(line.Dish).Dish.Name, line.Quantity)).ToList();
}
