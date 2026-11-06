namespace DeliveryManagement.Models;

// Base class for anyone who can log in. Holds the details everyone has
public abstract class User
{
    // User ID, like "C001"
    public string Id { get; set; }

    // Full name
    public string Name { get; set; }

    // Email used to log in
    public string Email { get; set; }

    // Phone number
    public string Phone { get; set; }

    // Password, kept as plain text (login security isn't part of this project)
    public string Password { get; set; }

    // Each type of user gives its own role name
    public abstract string GetRole();
}
