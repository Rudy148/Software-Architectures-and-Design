namespace DeliveryManagement.Models;

// A driver. Picks up parcels, takes the money and delivers
public class DeliveryStaff : User
{
    // The role name
    public override string GetRole()
    {
        return "Delivery Staff";
    }
}
