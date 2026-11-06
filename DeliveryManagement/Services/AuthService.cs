using DeliveryManagement.Data;
using DeliveryManagement.Models;

namespace DeliveryManagement.Services;

// Checks login details
public class AuthService
{
    private readonly DataStore _store;

    // Set up with where the users are kept
    public AuthService(DataStore store)
    {
        _store = store;
    }

    // Find the user with this email and password, or null
    public User Login(string email, string password)
    {
        // Look through every user. Email case doesn't matter
        foreach (User user in _store.GetAllUsers())
        {
            if (user.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && user.Password == password)
            {
                return user;
            }
        }

        return null;
    }
}
