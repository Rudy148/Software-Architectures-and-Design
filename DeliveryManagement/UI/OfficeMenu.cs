using DeliveryManagement.Models;
using DeliveryManagement.Services;

namespace DeliveryManagement.UI;

// Screens for office staff: send quotes, pick drivers and see requests
public class OfficeMenu
{
    // The main system and the logged-in staff member
    private readonly DeliveryManagementSystem _system;
    private readonly User _staff;

    // Make the menu for this staff member
    public OfficeMenu(DeliveryManagementSystem system, User staff)
    {
        _system = system;
        _staff = staff;
    }

    // Show the office menu until they log out
    public void Show()
    {
        // The menu choices
        string[] options =
        {
            "Issue a quote",
            "Allocate a driver",
            "Reassign a driver",
            "View all requests",
            "My messages",
        };

        while (true)
        {
            ConsoleUI.ShowTitle("Office Staff Menu - " + _staff.Name);
            ConsoleUI.ShowUnreadCount(_system, _staff.Id);
            // Ask what they want. 0 means log out
            int choice = ConsoleUI.Choose("What would you like to do?", options, "Log out");
            if (choice == 0)
            {
                return;
            }

            // Run the picked option. Errors are shown instead of crashing
            if (choice == 1)
            {
                ConsoleUI.RunSafely(IssueQuote);
            }
            else if (choice == 2)
            {
                ConsoleUI.RunSafely(AllocateDriver);
            }
            else if (choice == 3)
            {
                ConsoleUI.RunSafely(ReassignDriver);
            }
            else if (choice == 4)
            {
                ConsoleUI.RunSafely(ViewAllRequests);
            }
            else
            {
                ConsoleUI.ShowMessages(_system, _staff.Id);
            }
        }
    }

    // Work out a quote for a request and send it to the customer
    private void IssueQuote()
    {
        ConsoleUI.ShowTitle("Issue a Quote");
        // Requests that need a quote
        List<DeliveryRequest> waiting = _system.GetRequestsByStatus(RequestStatus.Requested, RequestStatus.Quoted);
        if (waiting.Count == 0)
        {
            ConsoleUI.ShowInfo("There are no requests waiting for a quote.");
            ConsoleUI.Pause();
            return;
        }

        Console.WriteLine("Requests waiting for a quote:");
        // Let staff pick one
        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference of the request to quote", waiting);
        if (request == null)
        {
            return;
        }

        ConsoleUI.ShowTitle("Issue a Quote for " + request.Id);
        ConsoleUI.ShowRequestDetails(request, _system);
        // Work out the price and show it
        Quote draft = _system.PrepareQuote(request.Id);
        ConsoleUI.ShowQuote(draft);

        // Send it as is, or change the price
        int choice = ConsoleUI.Choose("What would you like to do?", new[] { "Issue at the calculated price", "Change the price (reason needed)" }, "Cancel - do not issue");
        decimal newPrice = 0;
        string reason = "";

        // Changing the price needs a new price and a reason
        if (choice == 2)
        {
            string price = ConsoleUI.Ask("New price in dollars", input => InputValidator.CheckMoney(input, 1, QuoteCalculator.MaxPrice));
            reason = price == null ? null : ConsoleUI.Ask("Reason for the new price", InputValidator.CheckRequired);
            if (reason == null)
            {
                choice = 0;
            }
            else
            {
                newPrice = InputValidator.ParseNumber(price);
            }
        }

        // Check with staff before sending
        decimal finalPrice = newPrice == 0 ? draft.CalculatedAmount : newPrice;
        if (choice == 0 || !ConsoleUI.Confirm("Send a quote of $" + finalPrice.ToString("0.00") + " to " + _system.GetUserName(request.CustomerId) + "?"))
        {
            ConsoleUI.ShowInfo("No quote was issued.");
            ConsoleUI.Pause();
            return;
        }

        // Send it
        Quote quote = _system.IssueQuote(request.Id, draft, _staff.Id, newPrice, reason);
        ConsoleUI.ShowSuccess("Quote " + quote.Id + " for $" + quote.Amount.ToString("0.00") + " sent. The customer has been notified.");
        ConsoleUI.Pause();
    }

    // Give a waiting job to a driver
    private void AllocateDriver()
    {
        ConsoleUI.ShowTitle("Allocate a Driver");
        // Jobs that need a driver
        List<DeliveryRequest> waiting = _system.GetRequestsByStatus(RequestStatus.Accepted, RequestStatus.PickupFailed, RequestStatus.DeliveryFailed);
        if (waiting.Count == 0)
        {
            ConsoleUI.ShowInfo("There are no jobs waiting for a driver.");
            ConsoleUI.Pause();
            return;
        }

        Console.WriteLine("Jobs waiting for a driver:");
        // Pick a job, then a driver
        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference of the job to allocate", waiting);
        if (request == null)
        {
            return;
        }

        string driverId = ChooseDriver(request);
        if (driverId == null)
        {
            return;
        }

        // Check, then assign
        if (ConsoleUI.Confirm("Assign " + request.Id + " to " + _system.GetUserName(driverId) + "?"))
        {
            _system.AssignDriver(request.Id, driverId, _staff.Id);
            ConsoleUI.ShowSuccess(request.Id + " assigned to " + _system.GetUserName(driverId) + ". The driver and customer have been notified.");
        }
        else
        {
            ConsoleUI.ShowInfo("No driver was assigned.");
        }

        ConsoleUI.Pause();
    }

    // Move an assigned job to a different driver
    private void ReassignDriver()
    {
        ConsoleUI.ShowTitle("Reassign a Driver");
        // Jobs that already have a driver
        List<DeliveryRequest> assigned = _system.GetRequestsByStatus(RequestStatus.Assigned);
        if (assigned.Count == 0)
        {
            ConsoleUI.ShowInfo("There are no assigned jobs.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference of the job to reassign", assigned);
        if (request == null)
        {
            return;
        }

        // Show who has it now, then pick the new driver
        Console.WriteLine("Current driver: " + _system.GetUserName(request.DriverId));
        string driverId = ChooseDriver(request);
        if (driverId == null)
        {
            return;
        }

        // Check, then move it
        if (ConsoleUI.Confirm("Move " + request.Id + " to " + _system.GetUserName(driverId) + "?"))
        {
            _system.ReassignDriver(request.Id, driverId, _staff.Id);
            ConsoleUI.ShowSuccess(request.Id + " moved to " + _system.GetUserName(driverId) + ". Both drivers have been notified.");
        }
        else
        {
            ConsoleUI.ShowInfo("The job was not moved.");
        }

        ConsoleUI.Pause();
    }

    // Show drivers who can take the job and let staff pick one
    private string ChooseDriver(DeliveryRequest request)
    {
        List<DeliveryStaff> drivers = _system.GetAvailableDrivers(request.Id);
        Console.WriteLine();
        Console.WriteLine("Drivers for " + request.PickupDate.ToString("ddd dd/MM/yyyy") + " (least busy first):");
        // Everyone is full that day
        if (drivers.Count == 0)
        {
            ConsoleUI.ShowError("All drivers are fully booked on that day. Please contact the customer to agree a new pickup date.");
            ConsoleUI.Pause();
            return null;
        }

        // Show each driver and how busy they are
        foreach (DeliveryStaff driver in drivers)
        {
            Console.WriteLine(driver.Id + " - " + driver.Name + " (" + _system.CountDriverJobs(driver.Id, request.PickupDate) + " jobs that day)");
        }

        Console.WriteLine();
        // Ask for a driver ID from the list
        return ConsoleUI.Ask("ID of the driver to assign", input =>
        {
            bool found = drivers.Any(d => d.Id.Equals(input, StringComparison.OrdinalIgnoreCase));
            return found ? null : "Please enter one of the driver IDs listed above, e.g. " + drivers[0].Id + ".";
        })?.ToUpper();
    }

    // Show every request, and the details of one
    private void ViewAllRequests()
    {
        ConsoleUI.ShowTitle("All Requests");
        List<DeliveryRequest> requests = _system.GetAllRequests();
        if (requests.Count == 0)
        {
            ConsoleUI.ShowInfo("There are no requests yet.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference to see details", requests);
        if (request == null)
        {
            return;
        }

        ConsoleUI.ShowTitle("Request " + request.Id);
        ConsoleUI.ShowRequestDetails(request, _system);
        ConsoleUI.ShowHistory(request, _system);
        ConsoleUI.Pause();
    }
}
