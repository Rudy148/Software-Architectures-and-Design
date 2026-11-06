namespace DeliveryManagement.Models;

// A pickup or delivery address
public class Address
{
    // Street, like "12 Swanston St"
    public string Street { get; set; }

    // Suburb
    public string Suburb { get; set; }

    // 4-digit postcode
    public int Postcode { get; set; }

    // Show the address on one line
    public override string ToString()
    {
        return Street + ", " + Suburb + " VIC " + Postcode;
    }
}
