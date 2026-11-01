using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Support;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>The group pre-order page as the owner uses it: opening it, reading the participants and totals,
/// removing a guest and sending the shared order.</summary>
[Binding]
[Scope(Tag = "ui")]
public sealed class GroupOrderPageStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    // ---- actions ----

    [Given("the owner opens the group pre-order")]
    [When("the owner opens the group pre-order")]
    public Task WhenOwnerOpensThePage() => Actor.AttemptsToAsync(new OpenTheGroupPreOrder());

    [When("the owner refreshes the page")]
    public Task WhenOwnerRefreshes() => Actor.AttemptsToAsync(new RefreshTheGroupPreOrder());

    [When("the owner removes {string} on the page")]
    public Task WhenOwnerRemoves(string name) => Actor.AttemptsToAsync(new RemoveGuestOnThePage(name));

    [When("the owner starts removing {string} on the page but cancels")]
    public Task WhenOwnerCancelsRemoval(string name) => Actor.AttemptsToAsync(new RemoveGuestOnThePage(name, confirm: false));

    [When("the owner sends the pre-order on the page")]
    public Task WhenOwnerSendsThePreOrder() => Actor.AttemptsToAsync(new SendThePreOrderOnThePage());

    // ---- what the page shows ----

    [Then("the page shows {string} with {string} and a subtotal of {string}")]
    public async Task ThenPageShowsParticipant(string name, string dishes, string subtotal)
    {
        await EventuallyAsync(async () =>
        {
            var person = (await Actor.AsksForAsync(new TheGroupPreOrderShown())).Participant(Recall<GroupGuest>(name).DisplayName);

            var expected = TableReaders.Describe(DishNames.ByName(Actor, TableReaders.DishCounts(dishes)));
            await Assert.That(TableReaders.Describe(person.Dishes)).IsEqualTo(expected).Because($"dishes shown for {name}");
            await Assert.That(person.Subtotal).IsEqualTo(TableReaders.Money(subtotal)).Because($"subtotal shown for {name}");
        });
    }

    [Then("the page shows {string} without dishes and a subtotal of {string}")]
    public async Task ThenPageShowsParticipantWithoutDishes(string name, string subtotal)
    {
        await EventuallyAsync(async () =>
        {
            var person = (await Actor.AsksForAsync(new TheGroupPreOrderShown())).Participant(Recall<GroupGuest>(name).DisplayName);

            await Assert.That(person.Dishes).IsEmpty().Because($"dishes shown for {name}");
            await Assert.That(person.Subtotal).IsEqualTo(TableReaders.Money(subtotal));
        });
    }

    [Then("the page marks {string} as {string}")]
    public async Task ThenPageMarks(string name, string state)
    {
        await EventuallyAsync(async () =>
        {
            var person = (await Actor.AsksForAsync(new TheGroupPreOrderShown())).Participant(Recall<GroupGuest>(name).DisplayName);
            var chip = state switch
            {
                "confirmed" => "Submitted",
                "still choosing" => "Still choosing",
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Use 'confirmed' or 'still choosing'."),
            };

            await Assert.That(person.Status).IsEqualTo(chip);
        });
    }

    [Then("the page shows a grand total of {string}")]
    public async Task ThenPageShowsGrandTotal(string total)
    {
        await EventuallyAsync(async () =>
        {
            var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
            await Assert.That(page.GrandTotal).IsEqualTo(TableReaders.Money(total));
        });
    }

    [Then("the page shows no participants")]
    public async Task ThenPageShowsNoParticipants()
    {
        await EventuallyAsync(async () =>
        {
            var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
            await Assert.That(page.Participants.Count).IsEqualTo(0).Because($"the page shows: {string.Join(", ", page.Participants.Select(person => person.Title))}");
        });
    }

    [Then("the page does not show {string}")]
    public async Task ThenPageDoesNotShow(string name)
    {
        var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
        var displayName = Recall<GroupGuest>(name).DisplayName;
        await Assert.That(page.Participants.Any(person => person.Title.Contains(displayName, StringComparison.Ordinal))).IsFalse();
    }

    [Then("the page still shows {string}")]
    public async Task ThenPageStillShows(string name)
    {
        var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
        _ = page.Participant(Recall<GroupGuest>(name).DisplayName);
    }

    [Then("the page says the pre-order has been sent")]
    public async Task ThenPageSaysSent()
    {
        var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
        await Assert.That(page.SentNote).IsNotNull().Because("the page does not say the pre-order was sent");
        await Assert.That(page.CanSubmit).IsFalse();
    }

    [Then("the page says changes are no longer possible")]
    public async Task ThenPageSaysLocked()
    {
        var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
        await Assert.That(page.Notice).IsNotNull().Because("the page shows no notice");
        await Assert.That(page.Notice!).Contains("No changes can be applied");
    }

    [Then("the page offers no way to send the pre-order or to remove a guest")]
    public async Task ThenPageOffersNoChanges()
    {
        var page = await Actor.AsksForAsync(new TheGroupPreOrderShown());
        await Assert.That(page.CanSubmit).IsFalse().Because("the page offers to send the pre-order");
        await Assert.That(page.Participants.Any(person => person.CanBeRemoved)).IsFalse().Because("the page offers to remove a guest");
    }

    /// <summary>The page fills itself in from the service in the background, so right after a change made somewhere
    /// else (a guest adds a dish) it may still show the previous state. A check is repeated for a few seconds before
    /// it is allowed to fail; a page that really shows the wrong thing still fails, with the last difference.</summary>
    private async Task EventuallyAsync(Func<Task> check)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            try
            {
                await check();
                return;
            }
            catch (TUnit.Assertions.Exceptions.AssertionException) when (DateTime.UtcNow < deadline)
            {
                await NudgeThePageAsync();
            }
            catch (InvalidOperationException) when (DateTime.UtcNow < deadline)
            {
                await NudgeThePageAsync();
            }
        }
    }

    // Presses Refresh when the page offers it (it does not once the order is sent), then gives the answer a moment.
    private async Task NudgeThePageAsync()
    {
        var refresh = new Ui.GroupOrderPage(Actor.LookingAtThePage()).RefreshButton;
        if (await refresh.IsVisibleAsync())
            await refresh.ClickAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
