using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.Groups;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;

namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A customer registered and signed in through the API. Remembered under a role ("owner",
/// "another customer").</summary>
public sealed record CustomerSession(string Role, RegisterRequest Details, string IdToken);

/// <summary>A table booked by a customer, together with the group pre-order that comes with it. Remembered
/// under the role of the customer who booked it.</summary>
public sealed record GroupBooking(ReservationResponse Reservation, GuestInviteResponse Invite)
{
    public string GroupId => Invite.GroupReservationId;
}

/// <summary>A guest who joined a group. <paramref name="Name"/> is the short name the feature file uses,
/// <paramref name="DisplayName"/> the prefixed name the application shows. Remembered under the short name.</summary>
public sealed record GroupGuest(string Name, string DisplayName, string SessionId, string SessionToken);

/// <summary>A dish the scenario put on the menu, remembered under the short label the feature file uses.</summary>
public sealed record OfferedDish(string Label, DishResponse Dish);
