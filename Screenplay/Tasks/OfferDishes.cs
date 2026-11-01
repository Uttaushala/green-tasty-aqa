using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Puts dishes with known prices on the menu of the scenario's location, so totals can be worked out by
/// hand. Each dish gets a unique prefixed name and is remembered under its short label.</summary>
public sealed class OfferDishes(IReadOnlyList<DishOffer> offers) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var location = actor.Recall<LocationResponse>();
        var api = new DishesApi(actor.UsingTheApi());
        var waiter = await actor.AsksForAsync(new TheWaiterSession());

        foreach (var offer in offers)
        {
            var request = SampleDataFactory.CreateDishRequest([location.Id]) with
            {
                Name = $"{SampleDataFactory.Prefix}{offer.Dish}-{Guid.NewGuid().ToString("N")[..6]}",
                Price = offer.Price,
            };

            var response = actor.TheLastResponse();
            await response.CaptureAsync(await api.CreateDishAsync(request));
            if (!response.Succeeded)
                throw new InvalidOperationException($"Adding the dish '{offer.Dish}' failed with {response.Status}: {response.Body}");

            var dish = response.Json<DishResponse>();
            actor.Remember(offer.Dish, new OfferedDish(offer.Dish, dish));
            actor.Recall<CleanupScope>().TrackDelete(
                $"dish:{dish.Id}",
                () => api.DeleteDishAsync(dish.Id, force: true, bearerToken: waiter.IdToken));
        }
    }
}
