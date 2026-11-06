using System.Text.Json;
using System.Text.Json.Serialization;
using DeliveryManagement.Models;

namespace DeliveryManagement.Data;

// Keeps all the data and saves it to one JSON file. This is the only class that touches files
public class DataStore
{
    // The file path, the JSON settings, and the data itself
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private SavedData _data = new SavedData();

    // Set up the store for this file
    public DataStore(string filePath)
    {
        _filePath = filePath;
        // Make the JSON easy to read and save enums as words
        _options = new JsonSerializerOptions();
        _options.WriteIndented = true;
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    // Quick ways to get each list
    public List<Customer> Customers => _data.Customers;

    public List<OfficeStaff> OfficeStaff => _data.OfficeStaff;

    public List<DeliveryStaff> Drivers => _data.Drivers;

    public List<DeliveryRequest> Requests => _data.Requests;

    public List<Notification> Notifications => _data.Notifications;

    // Load the data from the file
    public void Load(bool startFresh)
    {
        // No file yet (or starting fresh), so start with just the starting accounts
        if (startFresh || !File.Exists(_filePath))
        {
            _data = new SavedData();
            AddStartingAccounts();
            Save();
            return;
        }

        // Otherwise read the file
        string json = File.ReadAllText(_filePath);
        _data = JsonSerializer.Deserialize<SavedData>(json, _options);
    }

    // Save all the data to the file
    public void Save()
    {
        string json = JsonSerializer.Serialize(_data, _options);
        File.WriteAllText(_filePath, json);
    }

    // Every user, whatever their role
    public List<User> GetAllUsers()
    {
        List<User> users = new List<User>();
        users.AddRange(Customers);
        users.AddRange(OfficeStaff);
        users.AddRange(Drivers);
        return users;
    }

    // Find a user by ID, or null
    public User FindUser(string id)
    {
        return GetAllUsers().FirstOrDefault(u => u.Id == id);
    }

    // Find a request by its reference (case doesn't matter), or null
    public DeliveryRequest FindRequest(string id)
    {
        return Requests.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    // Next tracking number, like "DR-1001"
    public string NextRequestId()
    {
        return "DR-" + (1001 + Requests.Count);
    }

    // Next quote number, like "Q-001"
    public string NextQuoteId()
    {
        int count = Requests.Sum(r => r.Quotes.Count);
        return "Q-" + (count + 1).ToString("000");
    }

    // Next payment number, like "P-001"
    public string NextPaymentId()
    {
        int count = Requests.Sum(r => r.Payments.Count);
        return "P-" + (count + 1).ToString("000");
    }

    // The users the system starts with (making accounts isn't part of this project)
    private void AddStartingAccounts()
    {
        // Two customers and one office staff member
        Customers.Add(new Customer { Id = "C001", Name = "Alice Nguyen", Email = "alice@example.com", Phone = "0412345678", Password = "alice123" });
        Customers.Add(new Customer { Id = "C002", Name = "Ben Carter", Email = "ben@example.com", Phone = "0423456789", Password = "ben123" });
        OfficeStaff.Add(new OfficeStaff { Id = "O001", Name = "Olivia Park", Email = "olivia@ywd.com.au", Phone = "0390001111", Password = "office123" });

        // Three drivers
        Drivers.Add(new DeliveryStaff { Id = "D001", Name = "Dan Murphy", Email = "dan@ywd.com.au", Phone = "0434567890", Password = "driver123" });
        Drivers.Add(new DeliveryStaff { Id = "D002", Name = "Priya Singh", Email = "priya@ywd.com.au", Phone = "0445678901", Password = "driver123" });
        Drivers.Add(new DeliveryStaff { Id = "D003", Name = "Sam Lee", Email = "sam@ywd.com.au", Phone = "0456789012", Password = "driver123" });
    }
}
