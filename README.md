# Green & Tasty — group pre-order test automation

## Direction and approach

**Direction 1 — UI tests of the main group pre-order scenarios plus API tests of authorization, validation and data set-up**, run against my own deployed Green & Tasty (backend + frontend).

The tests cover the owner's side of a group pre-order, on three levels:

| Level | Tag | What is checked |
|---|---|---|
| 1 | `@level-1` | The owner sees every participant with dishes, quantities, per-participant and grand totals; unconfirmed participants are marked; a participant without dishes; a group nobody joined; totals follow a guest's changes; the page is read-only less than 30 minutes before the reservation; only the owner may see the list (another customer, a guest, the waiter and an anonymous visitor are turned away); an unknown group is "not found". |
| 2 | `@level-2` | Sending the shared pre-order: only confirmed carts, equal dishes merged with summed quantity, the order and the group change state, the waiter sees the order; refused when there is nothing to send (nobody confirmed, nobody joined, an empty cart cannot be confirmed, the owner has no pre-order of their own), for an unknown group or reservation, for a second attempt and for anyone but the owner. |
| 3 | `@level-3` | Removing a guest: one of several (total recalculated), a guest who already confirmed (leaves the order too), the last guest, a guest of another group, an unknown guest, anyone but the owner; the same on the page, including cancelling the dialog; one end-to-end flow from the guests' choices to the waiter's order. |

Every level has positive and negative scenarios, UI (`@ui`) and API (`@api`) ones. The UI scenarios drive the real storefront in a browser; the API is used only to set up and tidy the data, to act as the guests (guests have no UI-free way in other than their invite link) and, in a few `Then` steps, to read back the state the page cannot show (the group's status, the waiter's order). Pure API scenarios check what the page cannot: who is allowed to do what, and what the service answers.

**Architecture: BDD (Reqnroll/Gherkin) + Screenplay on Playwright for .NET, TUnit runner** — the organizers' framework, extended rather than replaced. Feature files say what the owner wants and sees; step definitions are thin and talk to one collaborator, the actor; the actor performs *tasks* (`BookTheTable`, `JoinTheGroup`, `RemoveGuestOnThePage` …) and asks *questions* (`TheGroupPreOrderShown`, `TheParticipantList` …); only tasks and questions touch the API clients (`Api/`) and the page objects (`Ui/`). Why this stack: the same actor serves UI and API scenarios, the memory of the actor keeps parallel scenarios apart, and the layers make it cheap to add the next scenario.

What was added to the framework:

| Where | New |
|---|---|
| `Features/` | `GroupOrderParticipants.feature` (L1), `GroupOrderConfirmation.feature` (L2), `GroupOrderGuestRemoval.feature` (L3) |
| `StepDefinitions/` | `GroupOrderSetupStepDefinitions`, `GroupOrderApiStepDefinitions`, `GroupOrderPageStepDefinitions`, `GroupStepArguments` |
| `Screenplay/Tasks/` | `RegisterCustomer`, `BookTheTable`, `OfferDishes`, `JoinTheGroup`, `AddToTheCart`, `ConfirmTheCart`, `RequestTheParticipantList`, `ConfirmTheGroupPreOrder`, `RemoveGuestFromTheGroup`, `SignInAs`, `OpenTheGroupPreOrder`, `RefreshTheGroupPreOrder`, `RemoveGuestOnThePage`, `SendThePreOrderOnThePage` |
| `Screenplay/Questions/` | `TheGroupPreOrderShown`, `TheParticipantList`, `TheStateOfTheGroupPreOrder`, `TheOrderOfTheReservation` |
| `Api/` | `GroupOrdersApi`, `GuestSessionsApi`, DTOs in `Api/Dtos/Groups/` |
| `Ui/` | `GroupOrderPage` |

## Configure and run

Prerequisites: .NET 10 SDK and Node.js (Gherkin lint only). The browser is installed automatically on the first run.

```bash
cp .env.example .env     # then fill in the values below; .env is git-ignored and never committed
dotnet build
dotnet run --no-build -- --report-trx
```

| Setting | Meaning |
|---|---|
| `UI_URL` | where my frontend is served |
| `API_URL` | base URL of my backend API including the stage (for example `…/develop`) |

The group pre-order scenarios need no standing waiter account: every scenario creates its own location and its own waiter (the backend gives each reservation to a waiter of the table's location, and a waiter belongs to one location only), and removes both afterwards. `WAITER_*` and `ADMIN_*` are used only by the original framework scenarios (menu, locations, seeding, reports).

Everything else in `.env.example` is optional. Real environment variables win over `.env`, which is how CI is configured.

### Running a subset

```bash
# one level
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level-2]"
# UI only / API only
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=ui]"
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=api]"
# other useful tags: smoke, negative, e2e, view-mode, group-order
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=smoke]"
# several conditions: both level-3 and ui
dotnet run --no-build -- --treenode-filter "/*/*/*/*[(Category=level-3)&(Category=ui)]"
```

## Reports and artifacts

Everything a run produces is in `TestResults/` (git-ignored):

| What | Where |
|---|---|
| TUnit report and TRX (pass/fail per scenario) | `TestResults/*-report.html`, `TestResults/*.trx` |
| Reqnroll report (feature → scenario → step) | `TestResults/reqnroll/reqnroll_report.html` |
| Playwright trace of every failed UI scenario (screenshots, DOM snapshots, network, one group per Gherkin step) | `TestResults/traces/*.zip` — open with `npx playwright show-trace <file>.zip` |

In CI the same folders are uploaded as the artifacts `tunit-report`, `reqnroll-report` and `playwright-traces` of each run (Actions → AQA Tests → the run → Artifacts), and a summary table is written to the run page.

## Test data

* Every scenario builds its own world through the API: a location, a table, dishes with known prices, a waiter of its own, a freshly registered owner (unique e-mail), the booking, the owner's own pre-order (the service sends a group pre-order only into an existing order of the reservation), and guests with carts. Nothing depends on data that already exists, so scenarios are independent, run in any order and in parallel (`MAX_PARALLEL_TESTS`, default: CPU count, at most 4).
* Names created by the tests carry the `AQA-Sample-` prefix; dishes also carry a random suffix.
* Times are never written down: a booking is "starting in N minutes" and the start is `now + N` when the scenario runs (120 minutes normally, 20 minutes for the read-only scenario).
* Clean-up: each task registers its removal (reservation cancelled, dishes and location deleted) in the scenario's `CleanupScope`, which runs after every scenario, passed or failed; the framework's sweep removes leftover `AQA-Sample-` locations and dishes before and after a run. Waiters are removed from the waiter list; customers cannot be deleted through the API, they are recognisable by the prefix and a unique e-mail.

## Behaviour chosen for cases the requirements do not specify

| Case | What the tests expect |
|---|---|
| Sending the pre-order a second time | refused (a client error); the order created by the first send stays unchanged |
| Anyone but the owner (another customer, a guest of the group, the waiter, an anonymous visitor) lists participants, removes a guest or sends the pre-order | turned away (401 or 403); nothing changes |
| Removing the last guest | allowed; the group is then empty and its total is 0.00 |
| Sending when the owner has placed no order of their own | refused as invalid (400, "Order for reservation ... was not found"); the group stays open. The shared order is added to the owner's own pre-order, so scenarios place one first |
| Confirming an empty cart | refused as invalid (400, "The cart is empty"), so a confirmed cart always holds dishes |
| Sending when nobody confirmed a cart (or nobody joined) | refused; the group stays open and no order is created |
| Carts that are not confirmed | excluded from the sent order and from the page's grand total; the participant is still listed, marked "Still choosing", and counted in the API's list total |
| Removing a guest that belongs to another group | refused as invalid (400); the other group is untouched |
| Unknown group, reservation or guest | not found (404) |
| Less than 30 minutes before the reservation | the page is read-only: no Submit button, no remove buttons, a notice explains why |

Scenarios whose expected behaviour the application does not (yet) meet are to stay red and be tagged `@known-defect` with a comment, never ignored.

## Current result

Against my deployed environment the group pre-order scenarios (`@group-order`, 42 in total: levels 1, 2 and 3, UI and API) give **41 passed, 1 failed**. The one red scenario is a real defect of the service: *A second attempt is refused and the order stays as it was* (`@known-defect`): sending the group pre-order a second time returns 200 instead of a refusal. The same result is produced by the CI workflow (Actions -> AQA Tests); its summary and the artifacts `tunit-report` and `reqnroll-report` are attached to each run.

The scenarios of the original framework (locations, menu, registration, sign-in as a waiter and so on) need a standing waiter account (`WAITER_*`) and are not part of this homework; CI runs the `group-order` scope by default, the others can be started with the `scope` input of the workflow.

Not covered: the owner adding their own dishes through their own guest session.

## CI

* `.github/workflows/aqa-quality.yml` — on every push and pull request: no generated files tracked, format check, build with warnings as errors, Gherkin lint, test discovery.
* `.github/workflows/aqa-tests.yml` — the scenarios themselves, on every push to `main`, nightly and on demand (Actions → AQA Tests → Run workflow, with a scope or a filter such as `/*/*/*/*[Category=level-1]`). A failing test never stops the pipeline: reports, traces and the summary are published in steps that always run, and the job is marked failed at the very end.
* Configuration comes from the repository's **Variables** `AQA_UI_URL`, `AQA_API_URL` (nothing else is needed for the group pre-order scenarios; the optional `AQA_WAITER_*` / `AQA_ADMIN_*` secrets serve the original framework scenarios) (Settings → Secrets and variables → Actions). No credentials are stored in the repository.

---

# Framework reference

End-to-end tests for the Green & Tasty restaurant application: Reqnroll (Gherkin) + TUnit + Playwright for .NET, with the Screenplay pattern.

Guidance for extending the suite (writing Gherkin, steps, Screenplay, test data, working with an AI assistant) is in [CLAUDE.md](CLAUDE.md).

## Run the tests

```bash
cp .env.example .env        # then fill in your own values (see "Configuration")
dotnet build
dotnet run --no-build -- --report-trx                          # everything
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=smoke]"   # by tag
```

Everything a run produces lands in `TestResults/`: the TUnit report and TRX, the Reqnroll report in `reqnroll/`, and Playwright traces in `traces/` (open one with `npx playwright show-trace <file>.zip`).

Some scenarios are expected to fail (`@known-defect`, bugs in the application) or are skipped (`@wip`, no admin account configured). That is intentional.

## Quality gates

| Where | What runs |
|---|---|
| Git hooks (local) | `pre-commit`: `dotnet format` check and Gherkin lint on staged files · `commit-msg`: Conventional Commits · `pre-push`: build with analyzers |
| `aqa-quality.yml` | on every push and pull request: no generated files tracked, format, build (warnings are errors), Gherkin lint, test discovery |
| `aqa-tests.yml` | the scenarios themselves, nightly and on demand (Actions → AQA Tests → Run workflow) |

Enable the hooks once per clone, from the repository root:

```bash
dotnet tool restore --tool-manifest aqa/restaurant-aqa-framework/.config/dotnet-tools.json
git config core.hooksPath aqa/restaurant-aqa-framework/.husky
```

The hooks only act on commits and pushes that touch `aqa/restaurant-aqa-framework`. Gherkin lint needs Node.js.

## Configuration

There is no shared environment and no committed `.env`: copy `.env.example` to `.env` and fill it in with the data of **your own** deployment (see "Testing your own deployment"). `.env` is git-ignored; never commit it.

Settings come from `.env` or real environment variables (variables win):

| Setting | Meaning |
|---|---|
| `UI_URL` | where the frontend is served |
| `API_URL` | base URL of the backend API, including the stage (for example `…/develop`) |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | a waiter account that already exists in the environment under test |
| `TESTTABLE_ID`, `TESTTABLE_WAITER_*` | a table and the waiter assigned to it (only for scenarios that need them) |
| `ADMIN_USERNAME`, `ADMIN_PASSWORD` | an admin account; without it the admin scenarios are skipped |

The full list, with defaults, is in `.env.example`.

## Testing your own deployment

### If you deployed the backend / frontend / DevOps homework

1. `cp .env.example .env` and set `UI_URL` and `API_URL` to your deployment (API URL includes the stage, for example `…/develop`).
2. Waiters cannot be created by signing up, so create a waiter (and, for the admin scenarios, an admin) in *your* environment and put its credentials in `WAITER_*` / `ADMIN_*`. Optionally add a table and its assigned waiter (`TESTTABLE_*`).
3. Run `dotnet build` and a small slice first (`--treenode-filter "/*/*/*/*[Category=smoke]"`).

### If you have not deployed your own homework

If you deployed the original application, test that: set `UI_URL` / `API_URL` as above. Otherwise point the framework at a comparable public site (Booking, Airbnb, OpenTable, …) and write all the layers for it yourself: Gherkin features, step definitions, Screenplay tasks and questions, page objects with locators, and API clients where the site has a usable API. Use only flows you are allowed to automate (no purchases, no real bookings, respect the site's terms), and set `UI_URL` to that site.

1. **Generate a skeleton with Playwright codegen** (see "Presetup instructions"): record one flow, for example search → open a result → check details. Save the generated code to a scratch file, it is only raw material.
2. **Split it across the layers**: locators and small actions go to `Ui/` page objects, the flow becomes a task in `Screenplay/Tasks/`, the read-backs become questions in `Screenplay/Questions/`, the business wording goes to `Features/` and thin glue to `StepDefinitions/`.
3. **Replace weak locators.** Codegen often emits positional or text-fragile selectors. Prefer role + accessible name, label, test id, then CSS (see [CLAUDE.md](CLAUDE.md)); check them in the Playwright inspector.
4. Tag scenarios `@ui` / `@api`, keep them independent, and run `npx gherkin-lint -c .gherkin-lintrc "Features/*.feature"`.

### CI on your own public repository

Publish your work in your own public Git repository (the link is the deliverable). Workflows are disabled in a fresh fork until you enable them. Add the same values under Settings → Secrets and variables → Actions:

- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, `AQA_TESTTABLE_ID`
- **Secrets:** `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`, `AQA_ADMIN_USERNAME`, `AQA_ADMIN_PASSWORD`, `AQA_TESTTABLE_WAITER_USERNAME`, `AQA_TESTTABLE_WAITER_PASSWORD`

`aqa-quality.yml` runs on every push and pull request; `aqa-tests.yml` (nightly and on demand) tests whatever those variables point at. Never put credentials in the repository, in `.env.example` or in test code.

## Presetup instructions

This framework contains PlaywriteInstaller script so you don't need to setup it manually.
For full BDD highlighting in your IDE you may need to install Reqnroll extension https://docs.reqnroll.net/latest/ide-integrations/index.html
To start the Playwright code generator open Powershell in the framework directory and enter the line:
```bash
bin/Debug/net10.0/playwright.ps1 codegen "<your UI_URL or the public site you test>"
```
Build first so `playwright.ps1` exists. The URL is optional.
