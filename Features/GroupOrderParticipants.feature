@level-1 @group-order
Feature: Group pre-order participants

  The owner of a group reservation sees who has joined, what each participant chose and
  what it adds up to, before sending the shared pre-order. Only the owner may see it.

  Background:
    Given a location with a table for 6 guests
    And the restaurant offers these dishes:
      | dish  | price |
      | soup  | 8.50  |
      | steak | 21.00 |

  Rule: The owner sees every participant with their dishes and totals

    Background:
      Given the owner has booked the table for 4 guests starting in 120 minutes

    @ui @smoke
    Scenario: The owner sees each participant's dishes, quantities and subtotals
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | soup  | 1        |
        | steak | 1        |
      And the owner is signed in to the storefront
      When the owner opens the group pre-order
      Then the page shows "Taras" with "2 soup" and a subtotal of "17.00"
      And the page shows "Maria" with "1 soup, 1 steak" and a subtotal of "29.50"
      And the page shows a grand total of "46.50"

    @ui
    Scenario: A participant who has not confirmed is marked as still choosing
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Ivan" has joined the group and is still choosing:
        | dish  | quantity |
        | steak | 1        |
      And the owner is signed in to the storefront
      When the owner opens the group pre-order
      Then the page marks "Taras" as "confirmed"
      And the page marks "Ivan" as "still choosing"
      And the page shows "Ivan" with "1 steak" and a subtotal of "21.00"
      And the page shows a grand total of "17.00"

    @ui
    Scenario: A participant without dishes is shown without dishes
      Given "Taras" has joined the group without choosing any dishes
      And the owner is signed in to the storefront
      When the owner opens the group pre-order
      Then the page shows "Taras" without dishes and a subtotal of "0.00"
      And the page marks "Taras" as "still choosing"

    @ui
    Scenario: A group nobody has joined shows no participants
      Given the owner is signed in to the storefront
      When the owner opens the group pre-order
      Then the page shows no participants
      And the page shows a grand total of "0.00"

    @ui
    Scenario: The page follows the changes a guest makes to the cart
      Given "Taras" has joined the group and is still choosing:
        | dish | quantity |
        | soup | 1        |
      And the owner is signed in to the storefront
      And the owner opens the group pre-order
      When "Taras" adds 1 more of "soup" to the cart
      And the owner refreshes the page
      Then the page shows "Taras" with "2 soup" and a subtotal of "17.00"

    @api
    Scenario: The list of participants matches what each guest chose
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and is still choosing:
        | dish  | quantity |
        | steak | 1        |
      When the owner requests the list of participants
      Then the list of participants shows "Taras" with "2 soup" totalling "17.00"
      And the list of participants shows "Maria" with "1 steak" totalling "21.00"
      And the list of participants totals "38.00"

    @api
    Scenario: The total follows the changes a guest makes to the cart
      Given "Taras" has joined the group and is still choosing:
        | dish | quantity |
        | soup | 1        |
      When the owner requests the list of participants
      Then the list of participants totals "8.50"
      When "Taras" adds 1 more of "soup" to the cart
      And the owner requests the list of participants
      Then the list of participants shows "Taras" with "2 soup" totalling "17.00"
      And the list of participants totals "17.00"

    @api
    Scenario: A participant without dishes has an empty cart and no total
      Given "Taras" has joined the group without choosing any dishes
      When the owner requests the list of participants
      Then the list of participants shows "Taras" without dishes totalling "0.00"

    @api
    Scenario: A group nobody has joined has no participants
      When the owner requests the list of participants
      Then the list of participants is empty

  Rule: Nothing can be changed less than 30 minutes before the reservation

    Background:
      Given the owner has booked the table for 4 guests starting in 20 minutes

    @ui @view-mode
    Scenario: The owner can only look at the pre-order
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And the owner is signed in to the storefront
      When the owner opens the group pre-order
      Then the page shows "Taras" with "2 soup" and a subtotal of "17.00"
      And the page says changes are no longer possible
      And the page offers no way to send the pre-order or to remove a guest

  Rule: Only the owner of the group can see the list of participants

    Background:
      Given the owner has booked the table for 4 guests starting in 120 minutes

    @api @negative
    Scenario Outline: Someone else is turned away
      Given another customer is registered
      And "Taras" has joined the group without choosing any dishes
      When <requester> requests the list of participants
      Then access is denied

      Examples: People who are not the owner
        | requester            |
        | another customer     |
        | a guest of the group |
        | the waiter           |
        | an anonymous visitor |

    @api @negative
    Scenario: A group that does not exist cannot be looked at
      When the owner requests the list of participants of a group that does not exist
      Then the group could not be found
