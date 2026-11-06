namespace DeliveryManagement.Models;

// One message for a user
public class Notification
{
    // Who it's for
    public string UserId { get; set; }

    // The message text
    public string Message { get; set; }

    // When it was sent
    public DateTime Time { get; set; }

    // True once the user has read it
    public bool IsRead { get; set; }
}
