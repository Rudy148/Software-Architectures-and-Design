namespace DeliveryManagement.Models;

// What the customer said about a quote
public enum QuoteOutcome
{
    // Still waiting for an answer
    Pending,

    // Customer said yes
    Accepted,

    // Customer said no
    Rejected,

    // Replaced by a newer quote, but kept for records
    Superseded,
}

// One price offer for a request. A request can have a few, but old ones are never deleted
public class Quote
{
    // Quote number, like "Q-001"
    public string Id { get; set; }

    // The price the customer pays
    public decimal Amount { get; set; }

    // The price the system worked out
    public decimal CalculatedAmount { get; set; }

    // The lines that add up to the price
    public List<string> Breakdown { get; set; } = new List<string>();

    // When we expect to pick up
    public string PickupWindow { get; set; }

    // When we expect to deliver
    public string DeliveryWindow { get; set; }

    // Why staff changed the price (empty if they didn't)
    public string OverrideReason { get; set; } = "";

    // The staff member who sent it
    public string IssuedBy { get; set; }

    // When it was sent
    public DateTime IssuedAt { get; set; }

    // The customer's answer
    public QuoteOutcome Outcome { get; set; } = QuoteOutcome.Pending;
}
