using DeliveryManagement.Data;
using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// Gives jobs to drivers. Finds drivers who aren't full on the pickup day, least busy first
public class JobAllocator
{
    // The most jobs one driver can do in a day
    public const int MaxJobsPerDay = 5;

    private readonly DataStore _store;
    private readonly NotificationService _notifications;

    // Set up with the data and messages
    public JobAllocator(DataStore store, NotificationService notifications)
    {
        _store = store;
        _notifications = notifications;
    }

    // Count a driver's active jobs on one day
    public int CountJobs(string driverId, DateTime date)
    {
        return _store.Requests.Count(r => r.DriverId == driverId
            && r.PickupDate.Date == date.Date
            && (r.Status == RequestStatus.Assigned || r.Status == RequestStatus.PickedUp));
    }

    // Drivers who aren't full that day, least busy first.
    // When moving an assigned job, the driver who has it now is left out
    public List<DeliveryStaff> GetAvailableDrivers(DeliveryRequest request)
    {
        return _store.Drivers
            .Where(d => request.Status != RequestStatus.Assigned || d.Id != request.DriverId)
            .Where(d => CountJobs(d.Id, request.PickupDate) < MaxJobsPerDay)
            .OrderBy(d => CountJobs(d.Id, request.PickupDate))
            .ToList();
    }

    // Give a job to a driver
    public void AssignDriver(DeliveryRequest request, string driverId, string staffId)
    {
        // Only accepted jobs, or ones that failed before, can be given out
        if (request.Status != RequestStatus.Accepted
            && request.Status != RequestStatus.PickupFailed
            && request.Status != RequestStatus.DeliveryFailed)
        {
            throw new BusinessRuleException(request.Id + " is " + request.Status + " and cannot be allocated.");
        }

        CheckDriverIsAvailable(request, driverId);
        request.DriverId = driverId;

        // Failed delivery: we already have the parcels, so the driver only needs to deliver them again
        if (request.Status == RequestStatus.DeliveryFailed)
        {
            request.ChangeStatus(RequestStatus.PickedUp, staffId, "Redelivery given to driver " + driverId);
            _notifications.Notify(driverId, "Redelivery job " + request.Id + ": parcels are already picked up. Deliver to " + request.DeliveryAddress);
            _notifications.Notify(request.CustomerId, "A driver has been assigned to deliver " + request.Id + " again.");
            return;
        }

        // Give it to the driver, then tell the driver and the customer
        request.ChangeStatus(RequestStatus.Assigned, staffId, "Assigned to driver " + driverId);
        _notifications.Notify(driverId, "New job " + request.Id + ": pickup " + request.PickupDate.ToString("dd/MM/yyyy") + " from " + request.PickupAddress);
        _notifications.Notify(request.CustomerId, "A driver has been assigned to " + request.Id + ".");
    }

    // Move an assigned job to another driver. The change shows in the history
    public void ReassignDriver(DeliveryRequest request, string newDriverId, string staffId)
    {
        // Only jobs that already have a driver can be moved
        if (request.Status != RequestStatus.Assigned)
        {
            throw new BusinessRuleException(request.Id + " is " + request.Status + ". Only assigned jobs can be reassigned.");
        }

        CheckDriverIsAvailable(request, newDriverId);
        // Swap the driver, then tell both drivers
        string oldDriverId = request.DriverId;
        request.DriverId = newDriverId;
        request.ChangeStatus(RequestStatus.Assigned, staffId, "Reassigned from " + oldDriverId + " to " + newDriverId);
        _notifications.Notify(oldDriverId, "Job " + request.Id + " has been given to another driver.");
        _notifications.Notify(newDriverId, "New job " + request.Id + ": pickup " + request.PickupDate.ToString("dd/MM/yyyy") + " from " + request.PickupAddress);
    }

    // Stop if the driver can't take this job
    private void CheckDriverIsAvailable(DeliveryRequest request, string driverId)
    {
        bool available = GetAvailableDrivers(request).Any(d => d.Id == driverId);
        if (!available)
        {
            throw new BusinessRuleException("Driver " + driverId + " is not available on " + request.PickupDate.ToString("dd/MM/yyyy") + ".");
        }
    }
}
