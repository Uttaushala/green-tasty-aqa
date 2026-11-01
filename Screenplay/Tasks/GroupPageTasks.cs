using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Signs a registered customer (by role) in through the storefront's login form.</summary>
public sealed class SignInAs(string role) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var details = actor.Recall<CustomerSession>(role).Details;
        await actor.AttemptsToAsync(new SignInThroughTheLoginForm(details.Email, details.Password));

        // The storefront keeps the token at once but the profile ("user") only when /me has answered. A page opened
        // before that treats the visitor as not fully signed in and sends them to the main page, so wait for it.
        await actor.LookingAtThePage().WaitForFunctionAsync("() => localStorage.getItem('user') !== null");
    }
}

/// <summary>Opens the group pre-order page of the booking made by <paramref name="role"/> and waits until the
/// page has either shown the group or said why it cannot.</summary>
public sealed class OpenTheGroupPreOrder(string role = "owner") : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.NavigateAsync(actor.Recall<GroupBooking>(role).Reservation.Id);
        await page.GrandTotal.Or(page.NotFound).First.WaitForAsync();
    }
}

/// <summary>Presses the page's Refresh button and waits until the new list has been fetched and drawn.</summary>
public sealed class RefreshTheGroupPreOrder : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var browser = actor.LookingAtThePage();
        var page = new GroupOrderPage(browser);
        await browser.RunAndWaitForResponseAsync(
            () => page.RefreshButton.ClickAsync(),
            response => response.Request.Method == "GET" && response.Url.Contains("/sessions", StringComparison.Ordinal));
        await ThePageIsDrawn.WaitAsync(browser);
    }
}

/// <summary>The owner removes a guest on the page: presses the guest's remove button and then either confirms or
/// cancels the dialog. Confirming waits until the removal has been answered and drawn.</summary>
public sealed class RemoveGuestOnThePage(string guestName, bool confirm = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var browser = actor.LookingAtThePage();
        var page = new GroupOrderPage(browser);
        var displayName = actor.Recall<GroupGuest>(guestName).DisplayName;

        await page.ClickRemoveAsync(displayName);
        if (!confirm)
        {
            await page.CancelDialogAsync();
            return;
        }

        await browser.RunAndWaitForResponseAsync(
            () => page.ConfirmRemovalAsync(),
            response => response.Request.Method == "DELETE" && response.Url.Contains("/sessions/", StringComparison.Ordinal));
        await ThePageIsDrawn.WaitAsync(browser);
    }
}

/// <summary>The owner sends the shared pre-order on the page: presses Submit Pre-Order and confirms the dialog,
/// then waits until the send has been answered and drawn.</summary>
public sealed class SendThePreOrderOnThePage : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var browser = actor.LookingAtThePage();
        var page = new GroupOrderPage(browser);

        await page.ClickSubmitPreOrderAsync();
        await browser.RunAndWaitForResponseAsync(
            () => page.ConfirmSubmissionAsync(),
            response => response.Request.Method == "POST" && response.Url.Contains("/submit", StringComparison.Ordinal));
        await ThePageIsDrawn.WaitAsync(browser);
    }
}

/// <summary>After a response arrives the page still has to draw it; two animation frames are enough for that,
/// and unlike a sleep they do not depend on how fast the machine is.</summary>
internal static class ThePageIsDrawn
{
    public static Task WaitAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
}
