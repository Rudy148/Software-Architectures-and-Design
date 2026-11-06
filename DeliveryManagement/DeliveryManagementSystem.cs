using DeliveryManagement.Data;
using DeliveryManagement.Models;
using DeliveryManagement.Services;

namespace DeliveryManagement;

// The main class the menus talk to. It sets everything up and hands each job to the right helper (Facade)
public class DeliveryManagementSystem
{
    // Where all the data lives
    private readonly DataStore _store;
    // The helpers that do the real work
    private readonly AuthService _auth;
    private readonly NotificationService _notifications;
    private readonly QuoteCalculator _quoteCalculator;
    private readonly JobAllocator _jobAllocator;
    private readonly PaymentProcessor _paymentProcessor;

    // Set up the data and all the helpers when the app starts
    public DeliveryManagementSystem(string dataFile, bool startFresh)
    {
        // Load the data first, everything else needs it
        _store = new DataStore(dataFile);
        _store.Load(startFresh);

        // Now make the helpers. Messages come first because the others send messages
        _auth = new AuthService(_store);
        _notifications = new NotificationService(_store);
        _quoteCalculator = new QuoteCalculator(new StandardPricing(), _store, _notifications);
        _jobAllocator = new JobAllocator(_store, _notifications);
        _paymentProcessor = new PaymentProcessor(_store, _notifications);
    }

    // ----- Login, finding things, and messages -----

    // Log a user in. Gives null if the email or password is wrong
    public User Login(string email, string password)
    {
        return _auth.Login(email, password);
    }

    // Find a request by its reference, or show an error if it doesn't exist
    public DeliveryRequest GetRequest(string requestId)
    {
        DeliveryRequest request = _store.FindRequest(requestId);
        if (request == null)
        {
            throw new BusinessRuleException("No request found with reference " + requestId + ".");
        }

        return request;
    }

    // Get a user's name, or just the ID if we can't find them
    public string GetUserName(string userId)
    {
        User user = _store.FindUser(userId);
        return user == null ? userId : user.Name;
    }

    // Get requests with any of these statuses, soonest pickup first
    public List<DeliveryRequest> GetRequestsByStatus(params RequestStatus[] statuses)
    {
        return _store.Requests.Where(r => statuses.Contains(r.Status)).OrderBy(r => r.PickupDate).ToList();
    }

    // Get every request
    public List<DeliveryRequest> GetAllRequests()
    {
        return _store.Requests.ToList();
    }

    // Get all requests for one customer
    public List<DeliveryRequest> GetCustomerRequests(string customerId)
    {
        return _store.Requests.Where(r => r.CustomerId == customerId).ToList();
    }

    // Get all jobs for one driver, soonest pickup first
    public List<DeliveryRequest> GetDriverJobs(string driverId)
    {
        return _store.Requests.Where(r => r.DriverId == driverId).OrderBy(r => r.PickupDate).ToList();
    }

    // Count the messages the user hasn't read yet
    public int CountUnreadMessages(string userId)
    {
        return _notifications.GetMessages(userId).Count(n => !n.IsRead);
    }

    // Get a user's messages as text, then mark them as read
    public List<string> ReadMessages(string userId)
    {
        List<string> lines = new List<string>();
        foreach (Notification n in _notifications.GetMessages(userId))
        {
            // Put "(new)" in front of unread ones
            string marker = n.IsRead ? "" : "(new) ";
            lines.Add(marker + n.Time.ToString("dd/MM HH:mm") + " - " + n.Message);
        }

        // They've been seen now, so mark them read and save
        _notifications.MarkAllRead(userId);
        _store.Save();
        return lines;
    }

    // ----- Part 1: log, track and cancel requests -----

    // Save a new request and give back its tracking number
    public string LogRequest(DeliveryRequest request, string customerId)
    {
        // A request needs at least one parcel
        if (request.Parcels.Count == 0)
        {
            throw new BusinessRuleException("A request must have at least one parcel.");
        }

        // Give it a number, add it to the history and the list, then tell the office
        request.Id = _store.NextRequestId();
        request.CustomerId = customerId;
        request.AddNote(customerId, "Request logged");
        _store.Requests.Add(request);
        _notifications.NotifyOffice("New request " + request.Id + " is waiting for a quote.");
        _store.Save();
        return request.Id;
    }

    // Cancel a request before it gets picked up
    public void CancelRequest(string requestId, string customerId, string reason)
    {
        DeliveryRequest request = GetRequest(requestId);
        // Cancel it and tell the office
        request.Cancel(customerId, reason);
        _notifications.NotifyOffice(requestId + " was cancelled by the customer.");
        // If a driver already had it, tell them too
        if (request.DriverId != "")
        {
            _notifications.Notify(request.DriverId, "Job " + requestId + " was cancelled by the customer.");
        }

        _store.Save();
    }

    // ----- Part 2: quotes -----

    // Work out a price for staff to check first. Nothing is saved
    public Quote PrepareQuote(string requestId)
    {
        return _quoteCalculator.PrepareQuote(GetRequest(requestId));
    }

    // Send the quote to the customer. Staff can change the price here
    public Quote IssueQuote(string requestId, Quote draft, string staffId, decimal newPrice, string reason)
    {
        Quote quote = _quoteCalculator.IssueQuote(GetRequest(requestId), draft, staffId, newPrice, reason);
        _store.Save();
        return quote;
    }

    // Customer says yes to the quote
    public void AcceptQuote(string requestId, string customerId)
    {
        _quoteCalculator.AcceptQuote(GetRequest(requestId), customerId);
        _store.Save();
    }

    // Customer says no to the quote
    public void RejectQuote(string requestId, string customerId, string reason)
    {
        _quoteCalculator.RejectQuote(GetRequest(requestId), customerId, reason);
        _store.Save();
    }

    // ----- Part 3: drivers -----

    // Get drivers who can take the job on the pickup day, least busy first
    public List<DeliveryStaff> GetAvailableDrivers(string requestId)
    {
        return _jobAllocator.GetAvailableDrivers(GetRequest(requestId));
    }

    // Count a driver's jobs on one day
    public int CountDriverJobs(string driverId, DateTime date)
    {
        return _jobAllocator.CountJobs(driverId, date);
    }

    // Give a job to a driver
    public void AssignDriver(string requestId, string driverId, string staffId)
    {
        _jobAllocator.AssignDriver(GetRequest(requestId), driverId, staffId);
        _store.Save();
    }

    // Move a job to a different driver
    public void ReassignDriver(string requestId, string newDriverId, string staffId)
    {
        _jobAllocator.ReassignDriver(GetRequest(requestId), newDriverId, staffId);
        _store.Save();
    }

    // ----- Part 4: pickup, payment and delivery -----

    // How much the customer has to pay at pickup
    public decimal GetAmountDue(string requestId, string driverId)
    {
        return PaymentProcessor.GetAmountDue(GetRequest(requestId), driverId);
    }

    // Take a card payment (pretend, not real)
    public Payment PayByCard(string requestId, string driverId, string cardNumber)
    {
        Payment payment = _paymentProcessor.PayByCard(GetRequest(requestId), driverId, cardNumber);
        _store.Save();
        return payment;
    }

    // Take a cash payment
    public Payment PayByCash(string requestId, string driverId, decimal cashGiven)
    {
        Payment payment = _paymentProcessor.PayByCash(GetRequest(requestId), driverId, cashGiven);
        _store.Save();
        return payment;
    }

    // Mark the job as not paid so the pickup can still happen
    public void MarkUnpaid(string requestId, string driverId, string reason)
    {
        _paymentProcessor.MarkUnpaid(GetRequest(requestId), driverId, reason);
        _store.Save();
    }

    // Driver has the parcels, so tell the customer
    public void ConfirmPickup(string requestId, string driverId)
    {
        DeliveryRequest request = GetRequest(requestId);
        request.RecordPickup(driverId);
        _notifications.Notify(request.CustomerId, "Your parcels for " + requestId + " have been picked up.");
        _store.Save();
    }

    // Parcels weren't what was booked, so the office needs to quote again
    public void ReportParcelMismatch(string requestId, string driverId, string whatWasDifferent)
    {
        DeliveryRequest request = GetRequest(requestId);
        request.RecordParcelMismatch(driverId, whatWasDifferent);
        _notifications.NotifyOffice(requestId + ": parcels did not match at pickup (" + whatWasDifferent + "). Please re-quote.");
        _store.Save();
    }

    // Nobody was there at pickup, so tell the office and the customer
    public void ReportPickupFailed(string requestId, string driverId, string reason)
    {
        DeliveryRequest request = GetRequest(requestId);
        request.RecordPickupFailed(driverId, reason);
        _notifications.NotifyOffice(requestId + ": pickup failed (" + reason + "). Please re-allocate.");
        _notifications.Notify(request.CustomerId, "We could not pick up " + requestId + ". The office will contact you.");
        _store.Save();
    }

    // Parcels delivered. Save who got them and tell the customer
    public void ConfirmDelivery(string requestId, string driverId, string receivedBy)
    {
        DeliveryRequest request = GetRequest(requestId);
        request.RecordDelivery(driverId, receivedBy);
        _notifications.Notify(request.CustomerId, requestId + " was delivered. Received by " + receivedBy + ".");
        _store.Save();
    }

    // Delivery didn't work, so tell the office and the customer
    public void ReportDeliveryFailed(string requestId, string driverId, string reason)
    {
        DeliveryRequest request = GetRequest(requestId);
        request.RecordDeliveryFailed(driverId, reason);
        _notifications.NotifyOffice(requestId + ": delivery failed (" + reason + "). Please arrange redelivery.");
        _notifications.Notify(request.CustomerId, "We could not deliver " + requestId + " (" + reason + "). The office will contact you.");
        _store.Save();
    }
}
