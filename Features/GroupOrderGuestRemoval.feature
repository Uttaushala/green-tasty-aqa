@level-3 @group-order
Feature: Removing a guest from the group pre-order

  The owner can take a guest out of the group. Their dishes leave the totals and the shared
  order with them. Only the owner may do it, and only to guests of their own group.

  Background:
    Given a location with a table for 6 guests
    And the restaurant offers these dishes:
      | dish  | price |
      | soup  | 8.50  |
      | steak | 21.00 |
      | tea   | 3.00  |
    And the owner has booked the table for 4 guests starting in 120 minutes
    And the owner has placed a pre-order with:
      | dish | quantity |
      | tea  | 1        |

  Rule: A removed guest and their dishes are gone from the group

    @api @smoke
    Scenario: Removing one of several guests recalculates the total
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | steak | 1        |
      When the owner removes "Taras" from the group
      Then the request is accepted
      When the owner requests the list of participants
      Then the guests in the list are "Maria"
      And the list of participants shows "Maria" with "1 steak" totalling "21.00"
      And the list of participants totals "21.00"

    @api
    Scenario: A guest who confirmed leaves the shared order as well
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | steak | 1        |
      And the owner removes "Taras" from the group
      When the owner sends the group pre-order
      Then the owner sees an order with "1 steak, 1 tea"

    @api
    Scenario: The last guest can be removed
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 1        |
      When the owner removes "Taras" from the group
      And the owner requests the list of participants
      Then the list of participants has no guests
      And the list of participants totals "0.00"

  Rule: Only a guest of the owner's own group can be removed, and only by the owner

    @api @negative
    Scenario: A guest of another group cannot be removed
      Given another customer has booked a second table for 4 guests starting in 120 minutes
      And "Ivan" has joined the group of the other customer
      And "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 1        |
      When the owner removes "Ivan" from the group
      Then the request is refused as invalid
      When the owner requests the list of participants
      Then the guests in the list are "Taras"

    @api @negative
    Scenario: A guest that does not exist cannot be removed
      When the owner removes a guest that does not exist from the group
      Then the guest could not be found

    @api @negative
    Scenario Outline: Someone else cannot remove a guest
      Given another customer is registered
      And "Taras" has joined the group without choosing any dishes
      When <requester> removes "Taras" from the group
      Then access is denied
      When the owner requests the list of participants
      Then the guests in the list are "Taras"

      Examples: People who are not the owner
        | requester            |
        | another customer     |
        | a guest of the group |
        | the waiter           |
        | an anonymous visitor |

  Rule: The owner removes guests on the page

    @ui
    Scenario: The owner removes a guest and the page is recalculated
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | steak | 1        |
      And the owner is signed in to the storefront
      And the owner opens the group pre-order
      When the owner removes "Taras" on the page
      Then the page does not show "Taras"
      And the page shows "Maria" with "1 steak" and a subtotal of "21.00"
      And the page shows a grand total of "21.00"

    @ui @negative
    Scenario: Cancelling the removal keeps the guest
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And the owner is signed in to the storefront
      And the owner opens the group pre-order
      When the owner starts removing "Taras" on the page but cancels
      Then the page still shows "Taras"
      And the page shows a grand total of "17.00"

    @ui @e2e
    Scenario: From the guests' choices to the waiter's order
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | steak | 1        |
      And "Ivan" has joined the group and confirmed:
        | dish | quantity |
        | soup | 1        |
      And the owner is signed in to the storefront
      And the owner opens the group pre-order
      When the owner removes "Ivan" on the page
      And the owner sends the pre-order on the page
      Then the page says the pre-order has been sent
      And the waiter sees an order with "2 soup, 1 steak, 1 tea"
