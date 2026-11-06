namespace DeliveryManagement.Models;

// All the stages a request can be in
public enum RequestStatus
{
    // New, waiting for a quote
    Requested,

    // Quote sent, waiting for the customer
    Quoted,

    // Quote accepted, waiting for a driver
    Accepted,

    // Customer said no to the quote (the end)
    Rejected,

    // A driver has the job
    Assigned,

    // The driver has the parcels
    PickedUp,

    // Nobody was there at pickup
    PickupFailed,

    // Delivered (the end)
    Delivered,

    // Receiver wasn't home or said no
    DeliveryFailed,

    // Cancelled by the customer (the end)
    Cancelled,
}
