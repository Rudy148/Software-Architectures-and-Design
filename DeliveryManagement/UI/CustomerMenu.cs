using DeliveryManagement.Models;
using DeliveryManagement.Services;

namespace DeliveryManagement.UI;

// Screens for customers: make, track and cancel requests, and answer quotes
public class CustomerMenu
{
    // The main system and the logged-in customer
    private readonly DeliveryManagementSystem _system;
    private readonly User _customer;

    // Make the menu for this customer
    public CustomerMenu(DeliveryManagementSystem system, User customer)
    {
        _system = system;
        _customer = customer;
    }

    // Show the customer menu until they log out
    public void Show()
    {
        // The menu choices
        string[] options =
        {
            "Log a new delivery request",
            "Track my requests",
            "Respond to a quote",
            "Cancel a request",
            "My messages",
        };

        while (true)
        {
            ConsoleUI.ShowTitle("Customer Menu - " + _customer.Name);
            ConsoleUI.ShowUnreadCount(_system, _customer.Id);
            // Ask what they want. 0 means log out
            int choice = ConsoleUI.Choose("What would you like to do?", options, "Log out");
            if (choice == 0)
            {
                return;
            }

            // Run the picked option. Errors are shown instead of crashing
            if (choice == 1)
            {
                ConsoleUI.RunSafely(LogRequest);
            }
            else if (choice == 2)
            {
                ConsoleUI.RunSafely(TrackRequests);
            }
            else if (choice == 3)
            {
                ConsoleUI.RunSafely(RespondToQuote);
            }
            else if (choice == 4)
            {
                ConsoleUI.RunSafely(CancelRequest);
            }
            else
            {
                ConsoleUI.ShowMessages(_system, _customer.Id);
            }
        }
    }

    // Ask for an address. Gives null if the user goes back
    private static Address AskAddress(string which)
    {
        Address address = new Address();
        // Street, suburb, then postcode. Stop if they go back at any point
        address.Street = ConsoleUI.Ask(which + " street (e.g. 12 Swanston St)", InputValidator.CheckRequired);
        if (address.Street == null)
        {
            return null;
        }

        address.Suburb = ConsoleUI.Ask(which + " suburb", InputValidator.CheckName);
        if (address.Suburb == null)
        {
            return null;
        }

        string postcode = ConsoleUI.Ask(which + " postcode", InputValidator.CheckPostcode);
        if (postcode == null)
        {
            return null;
        }

        address.Postcode = int.Parse(postcode);
        return address;
    }

    // Let the customer add or remove parcels. Gives false if they cancel
    private static bool EditParcels(DeliveryRequest request)
    {
        while (true)
        {
            Console.WriteLine();
            // Show the parcels so far
            Console.WriteLine("Parcels in this request:");
            if (request.Parcels.Count == 0)
            {
                Console.WriteLine("  none yet");
            }

            for (int i = 0; i < request.Parcels.Count; i++)
            {
                Console.WriteLine("  " + (i + 1) + ". " + request.Parcels[i]);
            }

            // Ask what to do next
            int choice = ConsoleUI.Choose("Parcels:", new[] { "Add a parcel", "Remove a parcel", "Done with parcels" }, "Cancel");
            if (choice == 0)
            {
                return false;
            }

            if (choice == 1)
            {
                AddParcel(request);
            }
            else if (choice == 2)
            {
                RemoveParcel(request);
            }
            // Can't finish with no parcels
            else if (request.Parcels.Count == 0)
            {
                ConsoleUI.ShowError("Please add at least one parcel.");
            }
            else
            {
                return true;
            }
        }
    }

    // Ask for one parcel's details and add it
    private static void AddParcel(DeliveryRequest request)
    {
        // What it is, how heavy, and how big
        string description = ConsoleUI.Ask("What is in the parcel?", InputValidator.CheckRequired);
        if (description == null)
        {
            return;
        }

        string weight = ConsoleUI.Ask("Weight in kg (up to " + InputValidator.MaxWeightKg + ")", InputValidator.CheckWeight);
        if (weight == null)
        {
            return;
        }

        int size = ConsoleUI.Choose("Size:", new[] { "Small (satchel)", "Medium (moving box)", "Large (bigger than a moving box)" }, "Back");
        if (size == 0)
        {
            return;
        }

        // Make the parcel and add it to the request
        Parcel parcel = new Parcel();
        parcel.Description = description;
        parcel.WeightKg = InputValidator.ParseNumber(weight);
        parcel.Size = (ParcelSize)(size - 1);
        request.Parcels.Add(parcel);
        ConsoleUI.ShowSuccess("Parcel added.");
    }

    // Remove a parcel by its number in the list
    private static void RemoveParcel(DeliveryRequest request)
    {
        if (request.Parcels.Count == 0)
        {
            ConsoleUI.ShowError("There are no parcels to remove.");
            return;
        }

        // Ask which one, and make sure the number is in the list
        string number = ConsoleUI.Ask("Number of the parcel to remove", input =>
        {
            bool ok = int.TryParse(input, out int n) && n >= 1 && n <= request.Parcels.Count;
            return ok ? null : "Please enter a number from 1 to " + request.Parcels.Count + ".";
        });

        if (number != null)
        {
            request.Parcels.RemoveAt(int.Parse(number) - 1);
            ConsoleUI.ShowSuccess("Parcel removed.");
        }
    }

    // Walk the customer through making a new request
    private void LogRequest()
    {
        ConsoleUI.ShowTitle("Log a New Delivery Request");
        DeliveryRequest request = new DeliveryRequest();
        request.Id = "(new)";
        request.CustomerId = _customer.Id;

        // Ask for each detail. If they go back at any step, throw the request away
        request.PickupAddress = AskAddress("Pickup");
        if (request.PickupAddress == null)
        {
            Discarded();
            return;
        }

        request.DeliveryAddress = AskAddress("Delivery");
        // Delivering to the same place makes no sense, so ask again
        while (request.DeliveryAddress != null
            && request.DeliveryAddress.ToString().Equals(request.PickupAddress.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            ConsoleUI.ShowError("The delivery address can't be the same as the pickup address.");
            request.DeliveryAddress = AskAddress("Delivery");
        }

        if (request.DeliveryAddress == null)
        {
            Discarded();
            return;
        }

        request.RecipientName = ConsoleUI.Ask("Recipient name", InputValidator.CheckName);
        if (request.RecipientName == null)
        {
            Discarded();
            return;
        }

        request.RecipientPhone = ConsoleUI.Ask("Recipient phone", InputValidator.CheckPhone);
        if (request.RecipientPhone == null)
        {
            Discarded();
            return;
        }

        string date = ConsoleUI.Ask("Pickup date (dd/MM/yyyy)", InputValidator.CheckPickupDate);
        if (date == null)
        {
            Discarded();
            return;
        }

        // Save the date, then add the parcels
        request.PickupDate = InputValidator.ParseDate(date);
        if (!EditParcels(request))
        {
            Discarded();
            return;
        }

        // Let the customer check and fix things before saving
        while (true)
        {
            ConsoleUI.ShowTitle("Check Your Request");
            ConsoleUI.ShowRequestDetails(request, _system);
            string[] options = { "Submit request", "Change pickup date", "Change recipient", "Change parcels" };
            int choice = ConsoleUI.Choose("Is everything correct?", options, "Discard request");

            // Submit: save it and show the tracking number
            if (choice == 1)
            {
                string reference = _system.LogRequest(request, _customer.Id);
                ConsoleUI.ShowSuccess("Request logged. Your tracking reference is " + reference + ". We will send you a quote soon.");
                ConsoleUI.Pause();
                return;
            }
            // Change the pickup date
            else if (choice == 2)
            {
                string newDate = ConsoleUI.Ask("New pickup date (dd/MM/yyyy)", InputValidator.CheckPickupDate);
                if (newDate != null)
                {
                    request.PickupDate = InputValidator.ParseDate(newDate);
                }
            }
            // Change who gets the parcels
            else if (choice == 3)
            {
                string name = ConsoleUI.Ask("New recipient name", InputValidator.CheckName);
                string phone = name == null ? null : ConsoleUI.Ask("New recipient phone", InputValidator.CheckPhone);
                if (phone != null)
                {
                    request.RecipientName = name;
                    request.RecipientPhone = phone;
                }
            }
            // Change the parcels
            else if (choice == 4)
            {
                EditParcels(request);
            }
            // Throw it away, but check first
            else if (ConsoleUI.Confirm("Discard this request? Nothing will be saved"))
            {
                Discarded();
                return;
            }
        }
    }

    // Show the customer's requests and the details of one
    private void TrackRequests()
    {
        ConsoleUI.ShowTitle("Track My Requests");
        // Get their requests
        List<DeliveryRequest> requests = _system.GetCustomerRequests(_customer.Id);
        if (requests.Count == 0)
        {
            ConsoleUI.ShowInfo("You have no requests yet.");
            ConsoleUI.Pause();
            return;
        }

        // Let them pick one
        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference to see details", requests);
        if (request == null)
        {
            return;
        }

        // Show its details and history
        ConsoleUI.ShowTitle("Request " + request.Id);
        ConsoleUI.ShowRequestDetails(request, _system);
        ConsoleUI.ShowHistory(request, _system);
        ConsoleUI.Pause();
    }

    // Let the customer accept or reject a quote
    private void RespondToQuote()
    {
        ConsoleUI.ShowTitle("Respond to a Quote");
        // Only requests with a quote waiting
        List<DeliveryRequest> quoted = _system.GetCustomerRequests(_customer.Id)
            .Where(r => r.Status == RequestStatus.Quoted).ToList();
        if (quoted.Count == 0)
        {
            ConsoleUI.ShowInfo("You have no quotes waiting for an answer.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference of the quote to answer", quoted);
        if (request == null)
        {
            return;
        }

        // Show the request, the quote and the price
        Quote quote = request.GetCurrentQuote();
        ConsoleUI.ShowTitle("Quote " + quote.Id + " for " + request.Id);
        ConsoleUI.ShowRequestDetails(request, _system);
        ConsoleUI.ShowQuote(quote);
        Console.WriteLine("PRICE TO PAY: $" + quote.Amount.ToString("0.00") + " (paid to the driver at pickup, by card or cash)");
        Console.WriteLine();

        // Ask for their answer
        int choice = ConsoleUI.Choose("Your answer:", new[] { "Accept the quote", "Reject the quote" }, "Decide later");
        // Accept, after checking with them
        if (choice == 1 && ConsoleUI.Confirm("Accept $" + quote.Amount.ToString("0.00") + " for " + request.Id + "?"))
        {
            _system.AcceptQuote(request.Id, _customer.Id);
            ConsoleUI.ShowSuccess("Quote accepted. We will assign a driver and let you know.");
        }
        // Reject, with an optional reason
        else if (choice == 2)
        {
            string reason = ConsoleUI.AskOptional("Reason (optional, press Enter to skip)");
            if (reason != null && ConsoleUI.Confirm("Reject this quote? This closes the request"))
            {
                _system.RejectQuote(request.Id, _customer.Id, reason);
                ConsoleUI.ShowSuccess("Quote rejected. " + request.Id + " is now closed.");
            }
            else
            {
                ConsoleUI.ShowInfo("No answer recorded. You can answer this quote later.");
            }
        }
        else
        {
            ConsoleUI.ShowInfo("No answer recorded. You can answer this quote later.");
        }

        ConsoleUI.Pause();
    }

    // Let the customer cancel a request that hasn't been picked up
    private void CancelRequest()
    {
        ConsoleUI.ShowTitle("Cancel a Request");
        // Only requests that can still be cancelled
        List<DeliveryRequest> open = _system.GetCustomerRequests(_customer.Id)
            .Where(r => r.Status == RequestStatus.Requested || r.Status == RequestStatus.Quoted
                || r.Status == RequestStatus.Accepted || r.Status == RequestStatus.Assigned
                || r.Status == RequestStatus.PickupFailed)
            .ToList();
        if (open.Count == 0)
        {
            ConsoleUI.ShowInfo("You have no requests that can still be cancelled.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest request = ConsoleUI.ChooseRequest("Reference of the request to cancel", open);
        if (request == null)
        {
            return;
        }

        // Ask why, check they're sure, then cancel
        string reason = ConsoleUI.Ask("Reason for cancelling", InputValidator.CheckRequired);
        if (reason != null && ConsoleUI.Confirm("Cancel " + request.Id + "? This cannot be undone"))
        {
            _system.CancelRequest(request.Id, _customer.Id, reason);
            ConsoleUI.ShowSuccess(request.Id + " has been cancelled.");
        }
        else
        {
            ConsoleUI.ShowInfo("Nothing was cancelled.");
        }

        ConsoleUI.Pause();
    }

    // Tell the user nothing was saved
    private static void Discarded()
    {
        ConsoleUI.ShowInfo("Request discarded. Nothing was saved.");
        ConsoleUI.Pause();
    }
}
