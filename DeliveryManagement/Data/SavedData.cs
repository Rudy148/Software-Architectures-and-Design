using DeliveryManagement.Models;

namespace DeliveryManagement.Data;

// Everything that gets saved in the data file
public class SavedData
{
    // The customers
    public List<Customer> Customers { get; set; } = new List<Customer>();

    // The office staff
    public List<OfficeStaff> OfficeStaff { get; set; } = new List<OfficeStaff>();

    // The drivers
    public List<DeliveryStaff> Drivers { get; set; } = new List<DeliveryStaff>();

    // The requests
    public List<DeliveryRequest> Requests { get; set; } = new List<DeliveryRequest>();

    // The messages
    public List<Notification> Notifications { get; set; } = new List<Notification>();
}
