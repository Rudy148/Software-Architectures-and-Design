namespace DeliveryManagement.Models;

// One line in a request's history. Never changed once added
public class StatusRecord
{
    // The status after this step
    public RequestStatus Status { get; set; }

    // When it happened
    public DateTime Time { get; set; }

    // Who did it
    public string UserId { get; set; }

    // A short note about what happened
    public string Note { get; set; }
}
