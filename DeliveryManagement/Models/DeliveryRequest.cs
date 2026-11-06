namespace DeliveryManagement.Models;

// One delivery job from start to finish. Holds its parcels, quotes, payments and history, and checks its own status changes
public class DeliveryRequest
{
    // Which status can change to which. A table is easier than a big if/switch
    private static readonly Dictionary<RequestStatus, List<RequestStatus>> _allowedMoves =
        new Dictionary<RequestStatus, List<RequestStatus>>
        {
            { RequestStatus.Requested, new List<RequestStatus> { RequestStatus.Quoted, RequestStatus.Cancelled } },
            { RequestStatus.Quoted, new List<RequestStatus> { RequestStatus.Quoted, RequestStatus.Accepted, RequestStatus.Rejected, RequestStatus.Cancelled } },
            { RequestStatus.Accepted, new List<RequestStatus> { RequestStatus.Assigned, RequestStatus.Cancelled } },
            { RequestStatus.Assigned, new List<RequestStatus> { RequestStatus.Assigned, RequestStatus.PickedUp, RequestStatus.PickupFailed, RequestStatus.Requested, RequestStatus.Cancelled } },
            { RequestStatus.PickedUp, new List<RequestStatus> { RequestStatus.Delivered, RequestStatus.DeliveryFailed } },
            { RequestStatus.PickupFailed, new List<RequestStatus> { RequestStatus.Assigned, RequestStatus.Cancelled } },
            { RequestStatus.DeliveryFailed, new List<RequestStatus> { RequestStatus.PickedUp } },
            { RequestStatus.Rejected, new List<RequestStatus>() },
            { RequestStatus.Delivered, new List<RequestStatus>() },
            { RequestStatus.Cancelled, new List<RequestStatus>() },
        };

    // Tracking number, like "DR-1001"
    public string Id { get; set; }

    // The customer who booked it
    public string CustomerId { get; set; }

    // Where to pick up from
    public Address PickupAddress { get; set; }

    // Where to deliver to
    public Address DeliveryAddress { get; set; }

    // Who's getting the parcels
    public string RecipientName { get; set; }

    // Their phone number
    public string RecipientPhone { get; set; }

    // The pickup day
    public DateTime PickupDate { get; set; }

    // The parcels (at least one)
    public List<Parcel> Parcels { get; set; } = new List<Parcel>();

    // Every quote, oldest first
    public List<Quote> Quotes { get; set; } = new List<Quote>();

    // Every payment try, oldest first
    public List<Payment> Payments { get; set; } = new List<Payment>();

    // The driver on this job (empty if none)
    public string DriverId { get; set; } = "";

    // Where it's at now. Use ChangeStatus to change it
    public RequestStatus Status { get; set; } = RequestStatus.Requested;

    // Everything that happened, oldest first
    public List<StatusRecord> History { get; set; } = new List<StatusRecord>();

    // True if money is still owed
    public bool IsUnpaid { get; set; }

    // Who got the parcels at delivery
    public string ReceivedBy { get; set; } = "";

    // Move to a new status and add it to the history
    public void ChangeStatus(RequestStatus newStatus, string userId, string note)
    {
        // Stop if this move isn't in the table
        if (!_allowedMoves[Status].Contains(newStatus))
        {
            throw new BusinessRuleException(Id + " is " + Status + " and cannot change to " + newStatus + ".");
        }

        Status = newStatus;
        AddNote(userId, note);
    }

    // Add a line to the history without changing the status
    public void AddNote(string userId, string note)
    {
        StatusRecord record = new StatusRecord();
        record.Status = Status;
        record.Time = DateTime.Now;
        record.UserId = userId;
        record.Note = note;
        History.Add(record);
    }

    // The newest quote that hasn't been replaced, or null
    public Quote GetCurrentQuote()
    {
        return Quotes.LastOrDefault(q => q.Outcome != QuoteOutcome.Superseded);
    }

    // The quote the customer said yes to, or null
    public Quote GetAcceptedQuote()
    {
        return Quotes.LastOrDefault(q => q.Outcome == QuoteOutcome.Accepted);
    }

    // True if any payment went through
    public bool IsPaid()
    {
        return Payments.Any(p => p.Approved);
    }

    // Cancel the request. Only the owner can, and only before pickup (the table checks that)
    public void Cancel(string customerId, string reason)
    {
        if (CustomerId != customerId)
        {
            throw new BusinessRuleException(Id + " does not belong to you.");
        }

        // Once paid, the customer needs to call the office instead (refunds aren't part of this system)
        if (IsPaid())
        {
            throw new BusinessRuleException(Id + " has already been paid, so it can't be cancelled here. Please contact the office.");
        }

        ChangeStatus(RequestStatus.Cancelled, customerId, "Cancelled by customer: " + reason);
    }

    // Save the pickup. It must be paid or marked not paid first
    public void RecordPickup(string driverId)
    {
        CheckDriver(driverId);
        if (!IsPaid() && !IsUnpaid)
        {
            throw new BusinessRuleException("Take payment (or mark the job unpaid) before confirming pickup.");
        }

        ChangeStatus(RequestStatus.PickedUp, driverId, "Parcels picked up");
    }

    // Parcels weren't as booked. Go back to Requested and take the driver off so it can be quoted again
    public void RecordParcelMismatch(string driverId, string whatWasDifferent)
    {
        CheckDriver(driverId);
        // The parcels must be checked before taking payment, not after
        if (IsPaid())
        {
            throw new BusinessRuleException(Id + " has already been paid for these parcels, so it can't be sent back for a new quote.");
        }

        ChangeStatus(RequestStatus.Requested, driverId, "Parcels did not match booking: " + whatWasDifferent + ". New quote needed.");
        DriverId = "";
    }

    // Nobody was there at pickup
    public void RecordPickupFailed(string driverId, string reason)
    {
        CheckDriver(driverId);
        // If they paid, someone was there
        if (IsPaid())
        {
            throw new BusinessRuleException(Id + " has already been paid, so the pickup can't be marked as failed.");
        }

        ChangeStatus(RequestStatus.PickupFailed, driverId, "Pickup failed: " + reason);
    }

    // Save the delivery and who got the parcels
    public void RecordDelivery(string driverId, string receivedBy)
    {
        CheckDriver(driverId);
        ChangeStatus(RequestStatus.Delivered, driverId, "Delivered. Received by " + receivedBy);
        ReceivedBy = receivedBy;
    }

    // Delivery didn't work (not home or refused)
    public void RecordDeliveryFailed(string driverId, string reason)
    {
        CheckDriver(driverId);
        ChangeStatus(RequestStatus.DeliveryFailed, driverId, "Delivery failed: " + reason);
    }

    // Make sure this driver actually has the job
    private void CheckDriver(string driverId)
    {
        if (DriverId != driverId)
        {
            throw new BusinessRuleException(Id + " is not assigned to you.");
        }
    }
}
