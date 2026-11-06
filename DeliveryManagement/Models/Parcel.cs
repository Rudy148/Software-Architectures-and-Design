namespace DeliveryManagement.Models;

// The sizes a parcel can be
public enum ParcelSize
{
    // Fits in a satchel
    Small,

    // Up to a moving box
    Medium,

    // Bigger than a moving box
    Large,
}

// One item in a request
public class Parcel
{
    // What's inside
    public string Description { get; set; }

    // Weight in kg
    public decimal WeightKg { get; set; }

    // Its size
    public ParcelSize Size { get; set; }

    // Show the parcel on one line
    public override string ToString()
    {
        return Description + " (" + Size + ", " + WeightKg + " kg)";
    }
}
