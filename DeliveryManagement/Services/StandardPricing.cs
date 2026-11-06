using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// The normal price rule: base fee + parcel charges + travel charge + weekend extra
public class StandardPricing : IPricingStrategy
{
    // Fee on every request
    public const decimal BaseFee = 12.00m;

    // Cost per kg (rounded up to a whole kg)
    public const decimal RatePerKg = 1.50m;

    // Cost per zone travelled
    public const decimal RatePerZone = 6.00m;

    // Extra for a Saturday or Sunday pickup
    public const decimal WeekendSurcharge = 5.00m;

    // Work out the price, the breakdown and the time windows
    public Quote CreateDraftQuote(DeliveryRequest request)
    {
        Quote quote = new Quote();
        // Start with the base fee
        decimal total = BaseFee;
        quote.Breakdown.Add(Line("Base fee", BaseFee));

        // Add a size charge and a weight charge for each parcel
        foreach (Parcel parcel in request.Parcels)
        {
            decimal sizeCharge = GetSizeCharge(parcel.Size);
            decimal kilograms = Math.Ceiling(parcel.WeightKg);
            decimal weightCharge = kilograms * RatePerKg;
            quote.Breakdown.Add(Line(parcel.Description + " - " + parcel.Size + " size", sizeCharge));
            quote.Breakdown.Add(Line(parcel.Description + " - " + kilograms + " kg x $1.50", weightCharge));
            total = total + sizeCharge + weightCharge;
        }

        // Add the travel charge based on how many zones it crosses
        int fromZone = GetZone(request.PickupAddress.Postcode);
        int toZone = GetZone(request.DeliveryAddress.Postcode);
        int zonesTravelled = Math.Abs(fromZone - toZone) + 1;
        decimal travelCharge = zonesTravelled * RatePerZone;
        quote.Breakdown.Add(Line("Travel: zone " + fromZone + " to zone " + toZone, travelCharge));
        total = total + travelCharge;

        // Add the weekend extra if pickup is on a weekend
        DayOfWeek day = request.PickupDate.DayOfWeek;
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
        {
            quote.Breakdown.Add(Line("Weekend surcharge", WeekendSurcharge));
            total = total + WeekendSurcharge;
        }

        // Save the total and the pickup time
        quote.Amount = total;
        quote.CalculatedAmount = total;
        quote.PickupWindow = request.PickupDate.ToString("ddd dd/MM/yyyy") + ", 9am-12pm";

        // Short trips arrive the same afternoon, long ones the next morning
        if (zonesTravelled <= 2)
        {
            quote.DeliveryWindow = request.PickupDate.ToString("ddd dd/MM/yyyy") + ", 1pm-5pm";
        }
        else
        {
            quote.DeliveryWindow = request.PickupDate.AddDays(1).ToString("ddd dd/MM/yyyy") + ", 9am-1pm";
        }

        return quote;
    }

    // Zone of a postcode: 1 = inner (3000-3099), 2 = middle (3100-3199), 3 = outer
    public static int GetZone(int postcode)
    {
        if (postcode < 3100)
        {
            return 1;
        }

        if (postcode < 3200)
        {
            return 2;
        }

        return 3;
    }

    // Charge for each size
    private static decimal GetSizeCharge(ParcelSize size)
    {
        if (size == ParcelSize.Small)
        {
            return 3.00m;
        }

        if (size == ParcelSize.Medium)
        {
            return 6.00m;
        }

        return 10.00m;
    }

    // One line of the breakdown, lined up neatly
    private static string Line(string label, decimal amount)
    {
        return label + ": $" + amount.ToString("0.00");
    }
}
