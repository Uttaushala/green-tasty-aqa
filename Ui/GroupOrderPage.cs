using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui;

/// <summary>The owner's "Group Pre-Order" page (`/reservations/{id}/group-order`): who joined, what each person
/// chose, the consolidated order and the buttons to remove a guest or send the order. Locators and small actions
/// only; nothing here asserts.</summary>
public sealed class GroupOrderPage(IPage page) : BasePage(page)
{
    public ILocator Heading => Page.Locator("h1.page-title");
    public ILocator ParticipantCards => Page.Locator("app-participant-card");
    public ILocator GrandTotal => Page.Locator(".grand-total-value");
    public ILocator Notice => Page.Locator("p.notice");
    public ILocator SentNote => Page.Locator(".sent-note");
    public ILocator NotFound => Page.Locator(".state--error");
    public ILocator SubmitPreOrderButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order", Exact = true });
    public ILocator RefreshButton => Page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true });

    // The buttons of the two confirmation dialogs. The "Remove Guest" button on a participant card is named
    // "Remove <guest>" (aria-label), so the exact name below only ever matches the dialog's button.
    private ILocator DialogSubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit", Exact = true });
    private ILocator DialogRemoveButton => Page.GetByRole(AriaRole.Button, new() { Name = "Remove Guest", Exact = true });
    private ILocator DialogCancelButton => Page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true });

    public Task NavigateAsync(string reservationId) => NavigateToAsync($"/reservations/{reservationId}/group-order");

    public ILocator CardOf(string participantName) => ParticipantCards.Filter(new() { HasText = participantName });

    public ILocator RemoveButtonOf(string participantName) =>
        CardOf(participantName).GetByRole(AriaRole.Button, new() { Name = $"Remove {participantName}" });

    public Task ClickRemoveAsync(string participantName) => RemoveButtonOf(participantName).ClickAsync();

    public Task ConfirmRemovalAsync() => DialogRemoveButton.ClickAsync();

    public Task CancelDialogAsync() => DialogCancelButton.ClickAsync();

    public Task ClickSubmitPreOrderAsync() => SubmitPreOrderButton.ClickAsync();

    public Task ConfirmSubmissionAsync() => DialogSubmitButton.ClickAsync();
}
