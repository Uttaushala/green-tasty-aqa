using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Everything a group pre-order scenario needs to be true before it starts: a restaurant with a table
/// and a menu, an owner with a booking, guests with carts. All of it is created for this scenario alone, through
/// the API, and removed when the scenario ends. Times are given as "starting in N minutes", worked out at run time.</summary>
[Binding]
public sealed class GroupOrderSetupStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Given("a location with a table for {int} guests")]
    public Task GivenLocationWithTable(int seats) =>
        Actor.AttemptsToAsync(new SetUpLocationWithWaiter(), new AddTable(seats));

    [Given("the restaurant offers these dishes:")]
    public Task GivenRestaurantOffersDishes(DataTable dishes) =>
        Actor.AttemptsToAsync(new OfferDishes(TableReaders.OffersFrom(dishes)));

    [Given("the owner has booked the table for {int} guests starting in {int} minutes")]
    public Task GivenOwnerHasBooked(int guests, int minutes) =>
        Actor.AttemptsToAsync(new RegisterCustomer("owner"), new BookTheTable("owner", guests, minutes));

    [Given("the owner has placed a pre-order with:")]
    public Task GivenOwnerHasPlacedAPreOrder(DataTable dishes) =>
        Actor.AttemptsToAsync(new PlaceThePreOrder("owner", TableReaders.CartFrom(dishes)));

    [Given("another customer is registered")]
    public Task GivenAnotherCustomerIsRegistered() =>
        Actor.AttemptsToAsync(new RegisterCustomer("another customer"));

    [Given("another customer has booked a second table for {int} guests starting in {int} minutes")]
    public Task GivenAnotherCustomerHasBooked(int guests, int minutes) =>
        Actor.AttemptsToAsync(
            new RegisterCustomer("another customer"),
            new AddTable(6),
            new BookTheTable("another customer", guests, minutes));

    [Given("{string} has joined the group without choosing any dishes")]
    public Task GivenGuestHasJoined(string name) =>
        Actor.AttemptsToAsync(new JoinTheGroup(name));

    [Given("{string} has joined the group and confirmed:")]
    public Task GivenGuestHasConfirmed(string name, DataTable cart) =>
        Actor.AttemptsToAsync(new JoinTheGroup(name), new AddToTheCart(name, TableReaders.CartFrom(cart)), new ConfirmTheCart(name));

    [Given("{string} has joined the group and is still choosing:")]
    public Task GivenGuestIsStillChoosing(string name, DataTable cart) =>
        Actor.AttemptsToAsync(new JoinTheGroup(name), new AddToTheCart(name, TableReaders.CartFrom(cart)));

    [Given("{string} has joined the group of the other customer")]
    public Task GivenGuestHasJoinedTheOtherGroup(string name) =>
        Actor.AttemptsToAsync(new JoinTheGroup(name, groupOf: "another customer"));

    [Given("the owner has sent the group pre-order")]
    public async Task GivenOwnerHasSentThePreOrder()
    {
        await Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(Requester.Owner));
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"sending the pre-order as a precondition failed: {outcome.Status} {outcome.Body}");
    }

    [Given("the owner is signed in to the storefront")]
    public Task GivenOwnerIsSignedIn() => Actor.AttemptsToAsync(new SignInAs("owner"));
}
