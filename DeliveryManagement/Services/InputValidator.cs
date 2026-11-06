using System.Globalization;

namespace DeliveryManagement.Services;

// Checks what the user types. Each Check gives null if it's fine, or a message saying what's wrong
public static class InputValidator
{
    // Date format we use
    public const string DateFormat = "dd/MM/yyyy";

    // Heaviest parcel we carry, in kg
    public const decimal MaxWeightKg = 30m;

    // How far ahead a pickup can be booked
    public const int MaxDaysAhead = 60;

    // Melbourne postcode ranges we deliver to (first and last of each)
    private static readonly int[,] _melbournePostcodes =
    {
        { 3000, 3210 }, { 3335, 3341 }, { 3427, 3442 }, { 3750, 3810 }, { 3910, 3944 }, { 3975, 3978 },
    };

    // Can't be empty
    public static string CheckRequired(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "This field cannot be blank.";
        }

        return null;
    }

    // Names and suburbs: letters, spaces, - and ' only
    public static string CheckName(string input)
    {
        if (CheckRequired(input) != null)
        {
            return "This field cannot be blank.";
        }

        // Check every character
        foreach (char c in input)
        {
            if (!char.IsLetter(c) && c != ' ' && c != '-' && c != '\'')
            {
                return "Only letters, spaces, hyphens and apostrophes are allowed here.";
            }
        }

        return null;
    }

    // Phone: 10 digits starting with 0 (spaces are OK)
    public static string CheckPhone(string input)
    {
        string digits = input.Replace(" ", "");
        if (digits.Length != 10 || !digits.StartsWith('0') || !digits.All(char.IsDigit))
        {
            return "Phone must be 10 digits starting with 0, e.g. 0412 345 678.";
        }

        return null;
    }

    // Postcode: 4 digits and inside Melbourne
    public static string CheckPostcode(string input)
    {
        if (input.Length != 4 || !int.TryParse(input, out int postcode))
        {
            return "Postcode must be 4 digits, e.g. 3000.";
        }

        // See if it's in any of the ranges
        for (int i = 0; i < _melbournePostcodes.GetLength(0); i++)
        {
            if (postcode >= _melbournePostcodes[i, 0] && postcode <= _melbournePostcodes[i, 1])
            {
                return null;
            }
        }

        return "Sorry, " + postcode + " is outside our Greater Melbourne service area.";
    }

    // Weight: a number above 0 and not over the max
    public static string CheckWeight(string input)
    {
        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal weight))
        {
            return "Weight must be a number of kilograms, e.g. 2.5.";
        }

        if (weight <= 0 || weight > MaxWeightKg)
        {
            return "Weight must be more than 0 kg and at most " + MaxWeightKg + " kg.";
        }

        return null;
    }

    // Pickup date: dd/MM/yyyy, from today up to 60 days ahead
    public static string CheckPickupDate(string input)
    {
        if (!DateTime.TryParseExact(input, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return "Enter the date as dd/MM/yyyy, e.g. " + DateTime.Today.AddDays(3).ToString(DateFormat) + ".";
        }

        if (date < DateTime.Today)
        {
            return "The pickup date cannot be in the past.";
        }

        if (date > DateTime.Today.AddDays(MaxDaysAhead))
        {
            return "Pickups can be booked up to " + MaxDaysAhead + " days ahead.";
        }

        return null;
    }

    // Money: in range, with at most 2 decimal places
    public static string CheckMoney(string input, decimal min, decimal max)
    {
        if (!decimal.TryParse(input.TrimStart('$'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
        {
            return "Enter an amount in dollars, e.g. 25 or 25.50.";
        }

        if (decimal.Round(amount, 2) != amount)
        {
            return "Use at most 2 decimal places.";
        }

        if (amount < min || amount > max)
        {
            return "Enter an amount from $" + min.ToString("0.00") + " to $" + max.ToString("0.00") + ".";
        }

        return null;
    }

    // Card number: 16 digits (spaces are OK)
    public static string CheckCardNumber(string input)
    {
        string digits = input.Replace(" ", "");
        if (digits.Length != 16 || !digits.All(char.IsDigit))
        {
            return "Card number must be 16 digits.";
        }

        return null;
    }

    // Card expiry: MM/YY and not in the past
    public static string CheckExpiry(string input)
    {
        if (!DateTime.TryParseExact(input, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiry))
        {
            return "Enter the expiry as MM/YY, e.g. 08/28.";
        }

        DateTime firstOfThisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (expiry < firstOfThisMonth)
        {
            return "This card has expired.";
        }

        return null;
    }

    // Turn checked text into a date
    public static DateTime ParseDate(string input)
    {
        return DateTime.ParseExact(input, DateFormat, CultureInfo.InvariantCulture);
    }

    // Turn checked text into a number
    public static decimal ParseNumber(string input)
    {
        return decimal.Parse(input.TrimStart('$'), NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}
