using DeliveryManagement.Data;
using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// Works out and sends quotes, and saves the customer's answer. Staff can change the price if they give a reason
public class QuoteCalculator
{
    // The highest price staff can set by hand
    public const decimal MaxPrice = 5000m;

    // The price rule, the data, and messages
    private readonly IPricingStrategy _pricing;
    private readonly DataStore _store;
    private readonly NotificationService _notifications;

    // Set up with the price rule to use
    public QuoteCalculator(IPricingStrategy pricing, DataStore store, NotificationService notifications)
    {
        _pricing = pricing;
        _store = store;
        _notifications = notifications;
    }

    // Work out a price for staff to check. Nothing is saved
    public Quote PrepareQuote(DeliveryRequest request)
    {
        // Can only quote new or already quoted requests
        if (request.Status != RequestStatus.Requested && request.Status != RequestStatus.Quoted)
        {
            throw new BusinessRuleException(request.Id + " is " + request.Status + " and cannot be quoted.");
        }

        return _pricing.CreateDraftQuote(request);
    }

    // Send the quote to the customer
    public Quote IssueQuote(DeliveryRequest request, Quote draft, string staffId, decimal newPrice, string reason)
    {
        // If staff changed the price, check it and the reason
        if (newPrice != 0)
        {
            if (newPrice < 1 || newPrice > MaxPrice)
            {
                throw new BusinessRuleException("A new price must be between $1.00 and $" + MaxPrice + ".");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessRuleException("A reason is needed when changing the price.");
            }

            draft.Amount = newPrice;
            draft.OverrideReason = reason;
        }

        // Give it a number and mark who sent it
        draft.Id = _store.NextQuoteId();
        draft.IssuedBy = staffId;
        draft.IssuedAt = DateTime.Now;
        // Move the request to Quoted
        request.ChangeStatus(RequestStatus.Quoted, staffId, "Quote " + draft.Id + " issued: $" + draft.Amount.ToString("0.00"));

        // Older quotes are kept but marked as replaced
        foreach (Quote oldQuote in request.Quotes)
        {
            if (oldQuote.Outcome == QuoteOutcome.Pending || oldQuote.Outcome == QuoteOutcome.Accepted)
            {
                oldQuote.Outcome = QuoteOutcome.Superseded;
            }
        }

        // Add the new one and tell the customer
        request.Quotes.Add(draft);
        _notifications.Notify(request.CustomerId, "Your quote for " + request.Id + " is ready: $" + draft.Amount.ToString("0.00") + ". Please accept or reject it.");
        return draft;
    }

    // Customer said yes, so let the office know it needs a driver
    public void AcceptQuote(DeliveryRequest request, string customerId)
    {
        Quote quote = GetQuoteWaitingForAnswer(request, customerId);
        quote.Outcome = QuoteOutcome.Accepted;
        request.ChangeStatus(RequestStatus.Accepted, customerId, "Quote " + quote.Id + " accepted");
        _notifications.NotifyOffice(request.Id + ": quote accepted. Ready to allocate a driver.");
    }

    // Customer said no. This closes the request
    public void RejectQuote(DeliveryRequest request, string customerId, string reason)
    {
        Quote quote = GetQuoteWaitingForAnswer(request, customerId);
        quote.Outcome = QuoteOutcome.Rejected;
        request.ChangeStatus(RequestStatus.Rejected, customerId, "Quote " + quote.Id + " rejected. " + reason);
        _notifications.NotifyOffice(request.Id + ": quote rejected by the customer.");
    }

    // Get the quote waiting for an answer, and check it's this customer's
    private static Quote GetQuoteWaitingForAnswer(DeliveryRequest request, string customerId)
    {
        if (request.CustomerId != customerId)
        {
            throw new BusinessRuleException(request.Id + " does not belong to you.");
        }

        Quote quote = request.GetCurrentQuote();
        if (request.Status != RequestStatus.Quoted || quote == null || quote.Outcome != QuoteOutcome.Pending)
        {
            throw new BusinessRuleException(request.Id + " has no quote waiting for an answer.");
        }

        return quote;
    }
}
