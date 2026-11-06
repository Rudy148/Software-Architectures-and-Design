using DeliveryManagement.Data;
using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// Takes money at pickup by card or cash. Cards are pretend: one ending in 0000 is always declined. If the customer can't pay, the job is marked not paid and the office is told
public class PaymentProcessor
{
    private readonly DataStore _store;
    private readonly NotificationService _notifications;

    // Set up with the data and messages
    public PaymentProcessor(DataStore store, NotificationService notifications)
    {
        _store = store;
        _notifications = notifications;
    }

    // How much is owed (the accepted quote's price)
    public static decimal GetAmountDue(DeliveryRequest request, string driverId)
    {
        // The driver must have the job, it must be waiting for pickup, and not paid yet
        if (request.DriverId != driverId)
        {
            throw new BusinessRuleException(request.Id + " is not assigned to you.");
        }

        if (request.Status != RequestStatus.Assigned)
        {
            throw new BusinessRuleException("Payment is taken at pickup. " + request.Id + " is " + request.Status + ".");
        }

        if (request.IsPaid())
        {
            throw new BusinessRuleException(request.Id + " has already been paid.");
        }

        return request.GetAcceptedQuote().Amount;
    }

    // Charge a card (pretend). A declined card is saved, not an error, so the driver can try again
    public Payment PayByCard(DeliveryRequest request, string driverId, string cardNumber)
    {
        decimal amountDue = GetAmountDue(request, driverId);
        Payment payment = CreatePayment(request, driverId, PaymentMethod.Card, amountDue);
        // Only keep the last 4 digits
        payment.CardLastFour = cardNumber.Substring(cardNumber.Length - 4);

        // Cards ending in 0000 are always declined
        if (payment.CardLastFour == "0000")
        {
            payment.Approved = false;
            payment.DeclineReason = "Insufficient funds";
            request.AddNote(driverId, "Card ending " + payment.CardLastFour + " declined");
        }
        else
        {
            payment.Approved = true;
            RecordPaid(request, driverId, payment);
        }

        return payment;
    }

    // Take cash and work out the change
    public Payment PayByCash(DeliveryRequest request, string driverId, decimal cashGiven)
    {
        decimal amountDue = GetAmountDue(request, driverId);
        // Not enough cash
        if (cashGiven < amountDue)
        {
            throw new BusinessRuleException("The cash given is less than the amount due.");
        }

        Payment payment = CreatePayment(request, driverId, PaymentMethod.Cash, amountDue);
        // Save the cash and the change
        payment.CashGiven = cashGiven;
        payment.Change = cashGiven - amountDue;
        payment.Approved = true;
        RecordPaid(request, driverId, payment);
        return payment;
    }

    // Mark the job not paid so pickup can still happen, and tell the office
    public void MarkUnpaid(DeliveryRequest request, string driverId, string reason)
    {
        decimal amountDue = GetAmountDue(request, driverId);
        request.IsUnpaid = true;
        request.AddNote(driverId, "Marked unpaid ($" + amountDue.ToString("0.00") + " owed): " + reason);
        _notifications.NotifyOffice(request.Id + ": $" + amountDue.ToString("0.00") + " not collected at pickup. Reason: " + reason);
    }

    // Make a new payment and add it to the request
    private Payment CreatePayment(DeliveryRequest request, string driverId, PaymentMethod method, decimal amount)
    {
        Payment payment = new Payment();
        payment.Id = _store.NextPaymentId();
        payment.RequestId = request.Id;
        payment.Method = method;
        payment.Amount = amount;
        payment.TakenBy = driverId;
        payment.Time = DateTime.Now;
        request.Payments.Add(payment);
        return payment;
    }

    // Payment worked: add it to the history and tell the customer
    private void RecordPaid(DeliveryRequest request, string driverId, Payment payment)
    {
        request.IsUnpaid = false;
        request.AddNote(driverId, "Paid $" + payment.Amount.ToString("0.00") + " by " + payment.Method + ". Receipt " + payment.Id);
        _notifications.Notify(request.CustomerId, "Payment of $" + payment.Amount.ToString("0.00") + " received for " + request.Id + ". Receipt " + payment.Id + ".");
    }
}
