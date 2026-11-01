@level-2 @group-order
Feature: Sending the group pre-order

  The owner sends everything the confirmed participants chose to the restaurant as one
  shared order. The shared order is added to the pre-order the owner has already placed (here
  one tea), carts that were not confirmed are left out, and the order is sent once.

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

  Rule: The shared order holds what the confirmed participants chose

    @api @smoke
    Scenario: Dishes chosen by several participants are merged into one order
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | soup  | 1        |
        | steak | 1        |
      When the owner sends the group pre-order
      Then the request is accepted
      And the owner sees an order with "3 soup, 1 steak, 1 tea"
      And the order is marked as sent by the customer
      And the group pre-order is marked as sent

    @api
    Scenario: A cart that was not confirmed stays out of the order
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 1        |
      And "Ivan" has joined the group and is still choosing:
        | dish  | quantity |
        | steak | 3        |
      When the owner sends the group pre-order
      Then the request is accepted
      And the owner sees an order with "1 soup, 1 tea"

    @api
    Scenario: The waiter sees the order the owner sent
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      When the owner sends the group pre-order
      Then the waiter sees an order with "2 soup, 1 tea"

    @ui @e2e
    Scenario: The owner sends the shared order from the page
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And "Maria" has joined the group and confirmed:
        | dish  | quantity |
        | steak | 1        |
      And the owner is signed in to the storefront
      And the owner opens the group pre-order
      When the owner sends the pre-order on the page
      Then the page says the pre-order has been sent
      And the waiter sees an order with "2 soup, 1 steak, 1 tea"

  Rule: There must be something to send

    @api @negative
    Scenario: Nothing is sent while nobody has confirmed their cart
      Given "Taras" has joined the group and is still choosing:
        | dish | quantity |
        | soup | 1        |
      When the owner sends the group pre-order
      Then the request is refused
      And the group pre-order is still open
      And the order has not been sent and still holds "1 tea"

    @api @negative
    Scenario: Nothing is sent while nobody has joined
      When the owner sends the group pre-order
      Then the request is refused
      And the group pre-order is still open

    @api @negative
    Scenario: A group that does not exist cannot be sent
      When the owner sends the pre-order of a group that does not exist
      Then the group could not be found

    @api @negative
    Scenario: A reservation that does not exist cannot be sent
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 1        |
      When the owner sends the pre-order of a reservation that does not exist
      Then the reservation could not be found
      And the group pre-order is still open

  Rule: The pre-order is sent only once

    # Known defect: the service accepts a second send (200) instead of refusing it.
    @api @negative @known-defect
    Scenario: A second attempt is refused and the order stays as it was
      Given "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 2        |
      And the owner has sent the group pre-order
      When the owner sends the group pre-order
      Then the request is refused
      And the owner sees an order with "2 soup, 1 tea"

  Rule: Only the owner can send the pre-order

    @api @negative
    Scenario Outline: Someone else is turned away
      Given another customer is registered
      And "Taras" has joined the group and confirmed:
        | dish | quantity |
        | soup | 1        |
      When <requester> sends the group pre-order
      Then access is denied
      And the group pre-order is still open
      And the order has not been sent and still holds "1 tea"

      Examples: People who are not the owner
        | requester            |
        | another customer     |
        | a guest of the group |
        | the waiter           |
        | an anonymous visitor |
