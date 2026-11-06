using DeliveryManagement.Data;
using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// Saves in-app messages for customers, staff and drivers
public class NotificationService
{
    private readonly DataStore _store;

    // Set up with where messages are kept
    public NotificationService(DataStore store)
    {
        _store = store;
    }

    // Send a message to one user
    public void Notify(string userId, string message)
    {
        Notification notification = new Notification();
        notification.UserId = userId;
        notification.Message = message;
        notification.Time = DateTime.Now;
        _store.Notifications.Add(notification);
    }

    // Send a message to every office staff member
    public void NotifyOffice(string message)
    {
        foreach (OfficeStaff staff in _store.OfficeStaff)
        {
            Notify(staff.Id, message);
        }
    }

    // A user's messages, newest first
    public List<Notification> GetMessages(string userId)
    {
        return _store.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Time)
            .ToList();
    }

    // Mark all of a user's messages as read
    public void MarkAllRead(string userId)
    {
        foreach (Notification notification in GetMessages(userId))
        {
            notification.IsRead = true;
        }
    }
}
