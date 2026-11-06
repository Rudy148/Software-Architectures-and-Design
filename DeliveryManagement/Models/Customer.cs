namespace DeliveryManagement.Models;

// A person or business who books and pays for deliveries
public class Customer : User
{
    // The role name
    public override string GetRole()
    {
        return "Customer";
    }
}
