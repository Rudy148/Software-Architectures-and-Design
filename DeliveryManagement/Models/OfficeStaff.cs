namespace DeliveryManagement.Models;

// Office worker who sends quotes and gives jobs to drivers
public class OfficeStaff : User
{
    // The role name
    public override string GetRole()
    {
        return "Office Staff";
    }
}
