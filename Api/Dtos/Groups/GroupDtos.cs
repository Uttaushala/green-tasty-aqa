namespace Restaurant_Aqa_Framework.Api.Dtos.Groups;

/// <summary>GET /guest/invites/{token} and GET /reservations/{id}/group-reservation. The backend writes these
/// property names in PascalCase; <see cref="JsonDefaults"/> reads names case-insensitively.</summary>
public sealed record GuestInviteResponse(
    string GroupReservationId,
    string LocationId,
    string LocationAddress,
    DateTime StartTime,
    DateTime EndTime,
    int MaxGuestCount);

/// <summary>GET /reservations/{id}/invite-token.</summary>
public sealed record InviteTokenResponse(string Token);

/// <summary>POST /guest/sessions (request). The guest name is free text, so it carries the sample prefix.</summary>
public sealed record CreateGuestSessionRequest(string GroupReservationId, string GuestName);

/// <summary>POST /guest/sessions (response). The token is the guest's only credential: it is sent as a bearer
/// token on the guest's own calls and is not a Cognito token.</summary>
public sealed record CreateGuestSessionResponse(string SessionId, string SessionToken);

/// <summary>POST /guest/sessions/{id}/dishes.</summary>
public sealed record GuestDishRequest(string DishId, int Quantity);

/// <summary>The dish as the participant list shows it (the response carries more; only these matter here).</summary>
public sealed record GroupDish(string Id, string Name, decimal Price);

public sealed record ParticipantItem(GroupDish Dish, int Quantity);

/// <summary><paramref name="Type"/> is "Owner" or "Guest"; <paramref name="IsSubmitted"/> is true once the participant
/// confirmed their cart.</summary>
public sealed record Participant(
    string? Id,
    string Type,
    string Name,
    IReadOnlyList<ParticipantItem> Items,
    decimal Total,
    bool IsSubmitted);

/// <summary>GET /guest/groups/{groupReservationId}/sessions — and the answer of the guest-removal call.
/// <paramref name="Total"/> adds up every participant, confirmed or not.</summary>
public sealed record GroupSessionsResponse(IReadOnlyList<Participant> Participants, decimal Total);

/// <summary>GET /guest/groups/{groupReservationId}/status — <paramref name="Status"/> is "Open", "Submitted" or "Closed".</summary>
public sealed record GroupStatusResponse(int CurrentParticipants, int MaxCapacity, string Status);

/// <summary>POST /group-reservations/{groupReservationId}/submit (request).</summary>
public sealed record SubmitGroupRequest(string ReservationId);

/// <summary>POST /group-reservations/{groupReservationId}/submit (response item).</summary>
public sealed record SubmittedDish(string DishId, int Quantity);
