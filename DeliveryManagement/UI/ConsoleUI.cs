using DeliveryManagement.Models;

namespace DeliveryManagement.UI;

// The text screens: home screen, login, and helpers all the menus use. Typing "b" goes back
public class ConsoleUI
{
    // The main system
    private readonly DeliveryManagementSystem _system;

    // Make the screen
    public ConsoleUI(DeliveryManagementSystem system)
    {
        _system = system;
    }

    // Show the home screen until the user picks Exit
    public void Run()
    {
        while (true)
        {
            ShowTitle("Your Order We Deliver - Delivery Management System");
            // Log in or exit
            int choice = Choose("Welcome! What would you like to do?", new[] { "Log in" }, "Exit");
            if (choice == 0)
            {
                ShowTitle("Your Order We Deliver - Delivery Management System");
                ShowSuccess("All data has been saved. Goodbye!");
                return;
            }

            // Log in, then open the right menu for their role
            User user = LogIn();
            if (user is Customer)
            {
                new CustomerMenu(_system, user).Show();
            }
            else if (user is OfficeStaff)
            {
                new OfficeMenu(_system, user).Show();
            }
            else if (user is DeliveryStaff driver)
            {
                new DriverMenu(_system, driver).Show();
            }
        }
    }

    // ----- Helpers used by all menus -----

    // Clear the screen and show a title at the top
    public static void ShowTitle(string title)
    {
        // Only clear when it's a real screen
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }

        Console.WriteLine("# " + title);
        Console.WriteLine("(type b to go back)");
        Console.WriteLine();
    }

    // Keep asking until the answer passes the check. Gives null if the user types "b"
    public static string Ask(string label, Func<string, string> check)
    {
        while (true)
        {
            Console.Write(label + ": ");
            // Read the answer. null means the input has ended
            string input = Console.ReadLine();
            if (input == null)
            {
                return null;
            }

            input = input.Trim();
            // "b" means go back
            if (input.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Run the check. null means it's fine
            string error = check(input);
            if (error == null)
            {
                return input;
            }

            ShowError(error);
        }
    }

    // Ask a question that can be left empty
    public static string AskOptional(string label)
    {
        return Ask(label, input => null);
    }

    // Show a numbered list and give back the number picked. 0 means back
    public static int Choose(string question, string[] options, string backLabel)
    {
        Console.WriteLine(question);
        // Print each option with its number
        for (int i = 0; i < options.Length; i++)
        {
            Console.WriteLine((i + 1) + ". " + options[i]);
        }

        Console.WriteLine("0. " + backLabel);
        // Ask until they type a valid number
        string answer = Ask("Choose an option", input => CheckOption(input, options.Length));
        if (answer == null)
        {
            return 0;
        }

        return int.Parse(answer);
    }

    // Ask a yes/no question. Gives true for yes
    public static bool Confirm(string question)
    {
        string answer = Ask(question + " (y/n)", input =>
        {
            string lower = input.ToLower();
            return lower == "y" || lower == "n" ? null : "Please enter y or n.";
        });
        return answer != null && answer.ToLower() == "y";
    }

    // Show a list of requests and let the user type one's reference
    public static DeliveryRequest ChooseRequest(string label, List<DeliveryRequest> requests)
    {
        ShowRequestTable(requests);
        // Only accept references that are in the list
        string answer = Ask(label, input =>
        {
            bool found = requests.Any(r => r.Id.Equals(input, StringComparison.OrdinalIgnoreCase));
            return found ? null : "Please enter one of the references listed above, e.g. " + requests[0].Id + ".";
        });

        if (answer == null)
        {
            return null;
        }

        // Give back the one they picked
        return requests.First(r => r.Id.Equals(answer, StringComparison.OrdinalIgnoreCase));
    }

    // Show a short list of requests, one per line
    public static void ShowRequestTable(List<DeliveryRequest> requests)
    {
        foreach (DeliveryRequest r in requests)
        {
            Quote quote = r.GetCurrentQuote();
            string price = quote == null ? "no quote yet" : "$" + quote.Amount.ToString("0.00");
            Console.WriteLine(r.Id + " - " + r.PickupAddress.Suburb + " to " + r.DeliveryAddress.Suburb
                + " on " + r.PickupDate.ToString("dd/MM/yyyy") + ", " + r.Status + ", " + price);
        }

        Console.WriteLine();
    }

    // Show all the details of one request
    public static void ShowRequestDetails(DeliveryRequest request, DeliveryManagementSystem system)
    {
        Console.WriteLine("Reference: " + request.Id + " (" + request.Status + ")");
        Console.WriteLine("Customer: " + system.GetUserName(request.CustomerId));
        Console.WriteLine("Pickup: " + request.PickupAddress + " on " + request.PickupDate.ToString("ddd dd/MM/yyyy"));
        Console.WriteLine("Deliver to: " + request.DeliveryAddress);
        Console.WriteLine("Recipient: " + request.RecipientName + ", " + request.RecipientPhone);
        Console.WriteLine("Parcels:");
        for (int i = 0; i < request.Parcels.Count; i++)
        {
            Console.WriteLine("  " + (i + 1) + ". " + request.Parcels[i]);
        }

        // Show the driver if there is one
        if (request.DriverId != "")
        {
            Console.WriteLine("Driver: " + system.GetUserName(request.DriverId));
        }

        // Show the quote if there is one
        Quote quote = request.GetCurrentQuote();
        if (quote != null)
        {
            Console.WriteLine("Quote: " + quote.Id + ", $" + quote.Amount.ToString("0.00") + " (" + quote.Outcome + ")");
        }

        // Show if it's been paid or not
        if (request.IsPaid())
        {
            Console.WriteLine("Payment: paid");
        }
        else if (request.IsUnpaid)
        {
            Console.WriteLine("Payment: not paid, money still owed");
        }

        Console.WriteLine();
    }

    // Show everything that happened to a request, oldest first
    public static void ShowHistory(DeliveryRequest request, DeliveryManagementSystem system)
    {
        Console.WriteLine("History:");
        foreach (StatusRecord record in request.History)
        {
            Console.WriteLine("  " + record.Time.ToString("dd/MM HH:mm") + " " + record.Status + ": "
                + record.Note + " (" + system.GetUserName(record.UserId) + ")");
        }

        Console.WriteLine();
    }

    // Show how the price was worked out and the time windows
    public static void ShowQuote(Quote quote)
    {
        Console.WriteLine("Price breakdown:");
        // Each line of the price
        foreach (string line in quote.Breakdown)
        {
            Console.WriteLine("  " + line);
        }

        Console.WriteLine("Total: $" + quote.CalculatedAmount.ToString("0.00"));
        // If staff changed the price, show the new price and why
        if (quote.OverrideReason != "")
        {
            Console.WriteLine("Office changed the price to $" + quote.Amount.ToString("0.00") + " because: " + quote.OverrideReason);
        }

        Console.WriteLine("Pickup: " + quote.PickupWindow);
        Console.WriteLine("Delivery: " + quote.DeliveryWindow);
        Console.WriteLine();
    }

    // Show a user's messages and mark them as read
    public static void ShowMessages(DeliveryManagementSystem system, string userId)
    {
        ShowTitle("My Messages");
        List<string> messages = system.ReadMessages(userId);
        if (messages.Count == 0)
        {
            ShowInfo("You have no messages.");
        }

        foreach (string message in messages)
        {
            Console.WriteLine(message);
        }

        Console.WriteLine();
        Pause();
    }

    // Show how many new messages there are, if any
    public static void ShowUnreadCount(DeliveryManagementSystem system, string userId)
    {
        int unread = system.CountUnreadMessages(userId);
        if (unread > 0)
        {
            ShowInfo("You have " + unread + " new message(s).");
            Console.WriteLine();
        }
    }

    // Run a menu action. If a rule is broken, show the error instead of crashing
    public static void RunSafely(Action action)
    {
        try
        {
            action();
        }
        catch (BusinessRuleException ex)
        {
            ShowError(ex.Message);
            Pause();
        }
    }

    // Show an error
    public static void ShowError(string message)
    {
        Console.WriteLine("Error: " + message);
    }

    // Show that something worked
    public static void ShowSuccess(string message)
    {
        Console.WriteLine("Done. " + message);
    }

    // Show a normal message
    public static void ShowInfo(string message)
    {
        Console.WriteLine(message);
    }

    // Wait until the user presses Enter
    public static void Pause()
    {
        Console.Write("Press Enter to continue...");
        Console.ReadLine();
    }

    // Check the number is one of the options
    private static string CheckOption(string input, int optionCount)
    {
        if (int.TryParse(input, out int number) && number >= 0 && number <= optionCount)
        {
            return null;
        }

        return "Please enter a number from 0 to " + optionCount + ".";
    }

    // Ask for email and password until they're right. Gives null if the user goes back
    private User LogIn()
    {
        ShowTitle("Log In");
        while (true)
        {
            string email = Ask("Email", Services.InputValidator.CheckRequired);
            if (email == null)
            {
                return null;
            }

            string password = Ask("Password", Services.InputValidator.CheckRequired);
            if (password == null)
            {
                return null;
            }

            // Check the details
            User user = _system.Login(email, password);
            if (user != null)
            {
                return user;
            }

            ShowError("Email or password is incorrect. Please try again.");
        }
    }
}
