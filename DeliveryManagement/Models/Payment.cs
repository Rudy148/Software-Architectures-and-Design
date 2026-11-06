namespace DeliveryManagement.Models;

// How the customer paid
public enum PaymentMethod
{
    // Card (pretend, not a real payment)
    Card,

    // Cash given to the driver
    Cash,
}

// One payment try at pickup. Failed tries are kept too, and a good one is also the receipt
public class Payment
{
    // Payment number, also used as the receipt number
    public string Id { get; set; }

    // The request being paid for
    public string RequestId { get; set; }

    // Card or cash
    public PaymentMethod Method { get; set; }

    // How much was charged
    public decimal Amount { get; set; }

    // Cash handed over (cash only)
    public decimal CashGiven { get; set; }

    // Change given back (cash only)
    public decimal Change { get; set; }

    // Last 4 digits of the card (card only)
    public string CardLastFour { get; set; }

    // True if the money was taken
    public bool Approved { get; set; }

    // Why the card failed (empty if it worked)
    public string DeclineReason { get; set; } = "";

    // The driver who took it
    public string TakenBy { get; set; }

    // When it happened
    public DateTime Time { get; set; }
}
