using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>What people do with a group pre-order through the service, and what comes back. How an outcome shows
/// up on the wire (which status means "denied") is decided in this class, so feature files never mention it.</summary>
[Binding]
public sealed class GroupOrderApiStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    // ---- actions ----

    [When("{requester} requests the list of participants")]
    public Task WhenRequestsTheList(Requester who) => Actor.AttemptsToAsync(new RequestTheParticipantList(who));

    [When("{requester} requests the list of participants of a group that does not exist")]
    public Task WhenRequestsTheListOfUnknownGroup(Requester who) =>
        Actor.AttemptsToAsync(new RequestTheParticipantList(who, Guid.NewGuid().ToString()));

    [When("{string} adds {int} more of {string} to the cart")]
    public Task WhenGuestAddsMore(string name, int quantity, string dish) =>
        Actor.AttemptsToAsync(new AddToTheCart(name, [new CartLine(dish, quantity)]));

    [When("{string} confirms the cart")]
    public Task WhenGuestConfirmsTheCart(string name) => Actor.AttemptsToAsync(new ConfirmTheCart(name, mustSucceed: false));

    [When("{requester} sends the group pre-order")]
    public Task WhenSendsThePreOrder(Requester who) => Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(who));

    [When("{requester} sends the pre-order of a group that does not exist")]
    public Task WhenSendsThePreOrderOfUnknownGroup(Requester who) =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(who, groupId: Guid.NewGuid().ToString()));

    [When("{requester} sends the pre-order of a reservation that does not exist")]
    public Task WhenSendsThePreOrderOfUnknownReservation(Requester who) =>
        Actor.AttemptsToAsync(new ConfirmTheGroupPreOrder(who, reservationId: Guid.NewGuid().ToString()));

    [Given("{requester} removes {string} from the group")]
    [When("{requester} removes {string} from the group")]
    public Task WhenRemovesGuest(Requester who, string name) =>
        Actor.AttemptsToAsync(new RemoveGuestFromTheGroup(who, guestName: name));

    [When("the owner removes a guest that does not exist from the group")]
    public Task WhenRemovesUnknownGuest() =>
        Actor.AttemptsToAsync(new RemoveGuestFromTheGroup(Requester.Owner, guestSessionId: Guid.NewGuid().ToString()));

    // ---- the participant list ----

    [Then("the list of participants shows {string} with {string} totalling {string}")]
    public async Task ThenListShowsParticipant(string name, string dishes, string total)
    {
        var list = await Actor.AsksForAsync(new TheParticipantList());
        var person = ParticipantNamed(list.Participants, name);

        var shown = TableReaders.Describe(person.Items.Select(item => new CartLine(item.Dish.Name, item.Quantity)));
        var expected = TableReaders.Describe(DishNames.ByName(Actor, TableReaders.DishCounts(dishes)));
        await Assert.That(shown).IsEqualTo(expected).Because($"dishes of {name}");
        await Assert.That(person.Total).IsEqualTo(TableReaders.Money(total)).Because($"total of {name}");
    }

    [Then("the list of participants shows {string} without dishes totalling {string}")]
    public async Task ThenListShowsParticipantWithoutDishes(string name, string total)
    {
        var person = ParticipantNamed((await Actor.AsksForAsync(new TheParticipantList())).Participants, name);

        await Assert.That(person.Items).IsEmpty().Because($"dishes of {name}");
        await Assert.That(person.Total).IsEqualTo(TableReaders.Money(total));
    }

    [Then("the list of participants totals {string}")]
    public async Task ThenListTotals(string total)
    {
        var list = await Actor.AsksForAsync(new TheParticipantList());
        await Assert.That(list.Total).IsEqualTo(TableReaders.Money(total));
    }

    [Then("the list of participants is empty")]
    public async Task ThenListIsEmpty()
    {
        var list = await Actor.AsksForAsync(new TheParticipantList());
        await Assert.That(list.Participants).IsEmpty();
        await Assert.That(list.Total).IsEqualTo(0m);
    }

    [Then("the list of participants has no guests")]
    public async Task ThenListHasNoGuests()
    {
        var list = await Actor.AsksForAsync(new TheParticipantList());
        await Assert.That(list.Participants.Count(person => person.Type == "Guest")).IsEqualTo(0);
    }

    [Then("the guests in the list are {string}")]
    public async Task ThenGuestsInTheListAre(string names)
    {
        var list = await Actor.AsksForAsync(new TheParticipantList());
        var shown = list.Participants.Where(person => person.Type == "Guest").Select(person => person.Name).Order(StringComparer.Ordinal);
        var expected = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => Recall<GroupGuest>(name).DisplayName).Order(StringComparer.Ordinal);

        await Assert.That(string.Join(", ", shown)).IsEqualTo(string.Join(", ", expected));
    }

    // ---- outcomes ----

    [Then("access is denied")]
    public async Task ThenAccessIsDenied()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status is 401 or 403).IsTrue().Because($"expected the request to be turned away (401 or 403), got {outcome.Status}: {outcome.Body}");
    }

    [Then("the group could not be found")]
    [Then("the guest could not be found")]
    [Then("the reservation could not be found")]
    public async Task ThenCouldNotBeFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
    }

    [Then("the request is refused as invalid")]
    public async Task ThenRefusedAsInvalid()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400).Because($"response body was: {outcome.Body}");
    }

    [Then("the request is refused")]
    public async Task ThenRefused()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status is >= 400 and < 500).IsTrue().Because($"expected a refusal (4xx), got {outcome.Status}: {outcome.Body}");
    }

    [Then("the request is accepted")]
    public async Task ThenAccepted()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"response status {outcome.Status}, body: {outcome.Body}");
    }

    // ---- the shared order ----

    [Then("{requester} sees an order with {string}")]
    public async Task ThenSeesAnOrder(Requester who, string dishes)
    {
        var order = await Actor.AsksForAsync(new TheOrderOfTheReservation(who));
        await Assert.That(order).IsNotNull().Because("the restaurant has no order for the reservation");

        var shown = TableReaders.Describe(order!.OrderItems.Select(item => new CartLine(item.Dish.Name, item.Quantity)));
        var expected = TableReaders.Describe(DishNames.ByName(Actor, TableReaders.DishCounts(dishes)));
        await Assert.That(shown).IsEqualTo(expected);
    }

    [Then("the restaurant has no order for the reservation")]
    public async Task ThenNoOrder()
    {
        var order = await Actor.AsksForAsync(new TheOrderOfTheReservation(Requester.Owner));
        await Assert.That(order).IsNull();
    }

    [Then("the order has not been sent and still holds {string}")]
    public async Task ThenOrderNotSent(string dishes)
    {
        var order = await Actor.AsksForAsync(new TheOrderOfTheReservation(Requester.Owner));
        await Assert.That(order).IsNotNull().Because("the owner's pre-order is gone");
        await Assert.That(order!.Status).IsEqualTo(OrderStatus.Created);

        var shown = TableReaders.Describe(order.OrderItems.Select(item => new CartLine(item.Dish.Name, item.Quantity)));
        var expected = TableReaders.Describe(DishNames.ByName(Actor, TableReaders.DishCounts(dishes)));
        await Assert.That(shown).IsEqualTo(expected);
    }

    [Then("the order is marked as sent by the customer")]
    public async Task ThenOrderIsMarkedSent()
    {
        var order = await Actor.AsksForAsync(new TheOrderOfTheReservation(Requester.Owner));
        await Assert.That(order?.Status).IsEqualTo(OrderStatus.SubmittedByCustomer);
    }

    [Then("the group pre-order is marked as sent")]
    public async Task ThenGroupIsMarkedSent()
    {
        var state = await Actor.AsksForAsync(new TheStateOfTheGroupPreOrder());
        await Assert.That(state.Status).IsEqualTo("Submitted");
    }

    [Then("the group pre-order is still open")]
    public async Task ThenGroupIsStillOpen()
    {
        var state = await Actor.AsksForAsync(new TheStateOfTheGroupPreOrder());
        await Assert.That(state.Status).IsEqualTo("Open");
    }

    private Participant ParticipantNamed(IReadOnlyList<Participant> participants, string name)
    {
        var displayName = Recall<GroupGuest>(name).DisplayName;
        return participants.FirstOrDefault(person => person.Name == displayName)
            ?? throw new InvalidOperationException(
                $"The list does not show '{displayName}'. It shows: {string.Join(", ", participants.Select(person => person.Name))}.");
    }
}
