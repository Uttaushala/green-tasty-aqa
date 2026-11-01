@level-2 @group-order
Feature: Sending the group pre-order when there is nothing to build the order on

  The shared order is added to the owner's own pre-order and carries the dishes of confirmed
  carts. When either is missing the pre-order is refused and nothing changes.

  Background:
    Given a location with a table for 6 guests
    And the restaurant offers these dishes:
      | dish | price |
      | soup | 8.50  |
    And the owner has booked the table for 4 guests starting in 120 minutes

  @api @negative
  Scenario: A pre-order cannot be sent when the owner has not placed an order
    Given "Taras" has joined the group and confirmed:
      | dish | quantity |
      | soup | 1        |
    When the owner sends the group pre-order
    Then the request is refused as invalid
    And the group pre-order is still open
    And the restaurant has no order for the reservation

  @api @negative
  Scenario: An empty cart cannot be confirmed, so there is nothing to send
    Given the owner has placed a pre-order with:
      | dish | quantity |
      | soup | 1        |
    And "Taras" has joined the group without choosing any dishes
    When "Taras" confirms the cart
    Then the request is refused as invalid
    When the owner sends the group pre-order
    Then the request is refused as invalid
    And the group pre-order is still open
