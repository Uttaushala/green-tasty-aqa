using System.Globalization;
using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Support;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>One participant card as the owner sees it. <paramref name="Status"/> is the chip on the card
/// ("Submitted" or "Still choosing").</summary>
public sealed record ShownParticipant(
    string Title,
    string Status,
    IReadOnlyList<CartLine> Dishes,
    decimal Subtotal,
    bool CanBeRemoved);

/// <summary>Everything the group pre-order page shows. <paramref name="Notice"/> and <paramref name="SentNote"/> are
/// null when the page does not show them.</summary>
public sealed record ShownGroupPreOrder(
    IReadOnlyList<ShownParticipant> Participants,
    decimal GrandTotal,
    string? Notice,
    string? SentNote,
    bool CanSubmit)
{
    public ShownParticipant Participant(string displayName) =>
        Participants.FirstOrDefault(person => person.Title.Contains(displayName, StringComparison.Ordinal))
        ?? throw new InvalidOperationException(
            $"The page does not show '{displayName}'. It shows: {string.Join(", ", Participants.Select(person => person.Title))}.");
}

/// <summary>What the group pre-order page shows right now. The answer is a plain value, so the waiting for the page
/// to have loaded happens here, once, before anything is read.</summary>
public sealed class TheGroupPreOrderShown : IQuestion<ShownGroupPreOrder>
{
    public async Task<ShownGroupPreOrder> AnsweredByAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.GrandTotal.WaitForAsync();

        var participants = new List<ShownParticipant>();
        foreach (var card in await page.ParticipantCards.AllAsync())
        {
            var dishes = new List<CartLine>();
            foreach (var item in await card.Locator(".item").AllAsync())
            {
                var name = (await item.Locator(".name").TextContentAsync() ?? "").Trim();
                var quantity = (await item.Locator(".qty").TextContentAsync() ?? "").Replace("Qty:", "", StringComparison.Ordinal).Trim();
                dishes.Add(new CartLine(name, int.Parse(quantity, CultureInfo.InvariantCulture)));
            }

            participants.Add(new ShownParticipant(
                Title: (await card.Locator(".title").TextContentAsync() ?? "").Trim(),
                Status: (await card.Locator(".chip").TextContentAsync() ?? "").Trim(),
                Dishes: dishes,
                Subtotal: TableReaders.Money(await card.Locator(".subtotal span").Last.TextContentAsync() ?? "0"),
                CanBeRemoved: await card.Locator("button.remove").CountAsync() > 0));
        }

        return new ShownGroupPreOrder(
            participants,
            TableReaders.Money(await page.GrandTotal.TextContentAsync() ?? "0"),
            Notice: await TextOrNullAsync(page.Notice),
            SentNote: await TextOrNullAsync(page.SentNote),
            CanSubmit: await page.SubmitPreOrderButton.CountAsync() > 0);
    }

    private static async Task<string?> TextOrNullAsync(ILocator locator) =>
        await locator.CountAsync() > 0 ? (await locator.First.TextContentAsync())?.Trim() : null;
}
