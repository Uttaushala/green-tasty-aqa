using System.Text.Json.Serialization;

namespace Restaurant_Aqa_Framework.Api.Dtos.Orders;

/// <summary>Reads both the name ("SubmittedByCustomer") and the number the backend may send. Only ever read, never sent.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Created,
    Submitted,
    Cancelled,
    SubmittedByCustomer,
}
