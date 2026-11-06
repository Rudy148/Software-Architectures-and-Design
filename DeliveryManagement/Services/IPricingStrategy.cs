using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// Any price rule (Strategy). A new rule can be added without changing QuoteCalculator
public interface IPricingStrategy
{
    // Work out a quote for a request. It isn't saved
    Quote CreateDraftQuote(DeliveryRequest request);
}
