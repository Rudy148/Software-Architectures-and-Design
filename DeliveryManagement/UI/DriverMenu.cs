using DeliveryManagement.Models;
using DeliveryManagement.Services;

namespace DeliveryManagement.UI;

// Screens for drivers: see jobs, pick up and take payment, and deliver
public class DriverMenu
{
    // The main system and the logged-in driver
    private readonly DeliveryManagementSystem _system;
    private readonly DeliveryStaff _driver;

    // Make the menu for this driver
    public DriverMenu(DeliveryManagementSystem system, DeliveryStaff driver)
    {
        _system = system;
        _driver = driver;
    }

    // Show the driver menu until they log out
    public void Show()
    {
        // The menu choices
        string[] options =
        {
            "My jobs",
            "Pick up a job (and take payment)",
            "Deliver a job",
            "My messages",
        };

        while (true)
        {
            ConsoleUI.ShowTitle("Driver Menu - " + _driver.Name);
            ConsoleUI.ShowUnreadCount(_system, _driver.Id);
            // Ask what they want. 0 means log out
            int choice = ConsoleUI.Choose("What would you like to do?", options, "Log out");
            if (choice == 0)
            {
                return;
            }

            // Run the picked option. Errors are shown instead of crashing
            if (choice == 1)
            {
                ConsoleUI.RunSafely(ShowMyJobs);
            }
            else if (choice == 2)
            {
                ConsoleUI.RunSafely(PickUpJob);
            }
            else if (choice == 3)
            {
                ConsoleUI.RunSafely(DeliverJob);
            }
            else
            {
                ConsoleUI.ShowMessages(_system, _driver.Id);
            }
        }
    }

    // Print a receipt for a payment
    private static void ShowReceipt(Payment payment, string driverName)
    {
        Console.WriteLine();
        Console.WriteLine("# Receipt " + payment.Id);
        Console.WriteLine("Request: " + payment.RequestId);
        Console.WriteLine("Date: " + payment.Time.ToString("dd/MM/yyyy HH:mm"));
        // Show how they paid
        if (payment.Method == PaymentMethod.Card)
        {
            Console.WriteLine("Paid by: card ending " + payment.CardLastFour);
        }
        else
        {
            Console.WriteLine("Paid by: cash");
        }

        Console.WriteLine("Amount: $" + payment.Amount.ToString("0.00"));
        // For cash, also show the cash given and the change
        if (payment.Method == PaymentMethod.Cash)
        {
            Console.WriteLine("Cash given: $" + payment.CashGiven.ToString("0.00"));
            Console.WriteLine("Change: $" + payment.Change.ToString("0.00"));
        }

        Console.WriteLine("Driver: " + driverName);
        Console.WriteLine();
    }

    // Show the driver's jobs and the details of one
    private void ShowMyJobs()
    {
        ConsoleUI.ShowTitle("My Jobs");
        List<DeliveryRequest> jobs = _system.GetDriverJobs(_driver.Id);
        if (jobs.Count == 0)
        {
            ConsoleUI.ShowInfo("You have no jobs.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest job = ConsoleUI.ChooseRequest("Reference to see details", jobs);
        if (job == null)
        {
            return;
        }

        ConsoleUI.ShowTitle("Job " + job.Id);
        ConsoleUI.ShowRequestDetails(job, _system);
        ConsoleUI.ShowHistory(job, _system);
        ConsoleUI.Pause();
    }

    // Go through a pickup step by step
    private void PickUpJob()
    {
        ConsoleUI.ShowTitle("Pick Up a Job");
        // Only jobs waiting to be picked up
        List<DeliveryRequest> jobs = _system.GetDriverJobs(_driver.Id)
            .Where(j => j.Status == RequestStatus.Assigned).ToList();
        if (jobs.Count == 0)
        {
            ConsoleUI.ShowInfo("You have no jobs waiting for pickup.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest job = ConsoleUI.ChooseRequest("Reference of the job to pick up", jobs);
        if (job == null)
        {
            return;
        }

        ConsoleUI.ShowTitle("Pick Up " + job.Id);
        ConsoleUI.ShowRequestDetails(job, _system);

        // Step 1: is someone at the address?
        int choice = ConsoleUI.Choose("At the pickup address:", new[] { "Customer is here", "Nobody is here" }, "Back");
        if (choice == 0)
        {
            return;
        }

        // Nobody there, so record a failed pickup
        if (choice == 2)
        {
            string reason = ConsoleUI.Ask("What happened?", InputValidator.CheckRequired);
            if (reason != null && ConsoleUI.Confirm("Record that the pickup failed?"))
            {
                _system.ReportPickupFailed(job.Id, _driver.Id, reason);
                ConsoleUI.ShowSuccess("Pickup failure recorded. The office and customer have been told.");
                ConsoleUI.Pause();
            }

            return;
        }

        // Step 2: are the parcels the same as booked? If not, send it back for a new quote
        if (!ConsoleUI.Confirm("Do the parcels match the booking above?"))
        {
            string difference = ConsoleUI.Ask("What is different?", InputValidator.CheckRequired);
            if (difference != null && ConsoleUI.Confirm("Send the job back to the office for a new quote?"))
            {
                _system.ReportParcelMismatch(job.Id, _driver.Id, difference);
                ConsoleUI.ShowSuccess(job.Id + " sent back to the office for a new quote. Do not take the parcels.");
                ConsoleUI.Pause();
            }

            return;
        }

        // Step 3: take the money, unless it's already done
        if (!job.IsPaid() && !job.IsUnpaid && !TakePayment(job))
        {
            ConsoleUI.ShowInfo("Pickup not confirmed. You can come back to this job.");
            ConsoleUI.Pause();
            return;
        }

        // Step 4: finish the pickup
        _system.ConfirmPickup(job.Id, _driver.Id);
        ConsoleUI.ShowSuccess("Pickup confirmed. " + job.Id + " is now Picked Up.");
        ConsoleUI.Pause();
    }

    // Take card or cash, or mark the job not paid. Gives false if the driver goes back
    private bool TakePayment(DeliveryRequest job)
    {
        // Work out how much is owed
        decimal amountDue = _system.GetAmountDue(job.Id, _driver.Id);
        // Keep going until a payment works or the driver goes back
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("AMOUNT DUE: $" + amountDue.ToString("0.00"));
            string[] options = { "Card", "Cash", "Customer cannot pay - mark unpaid and continue" };
            int choice = ConsoleUI.Choose("How is the customer paying?", options, "Back");
            if (choice == 0)
            {
                return false;
            }

            if (choice == 1 && PayByCard(job, amountDue))
            {
                return true;
            }

            if (choice == 2 && PayByCash(job, amountDue))
            {
                return true;
            }

            if (choice == 3 && MarkUnpaid(job))
            {
                return true;
            }
        }
    }

    // Take a card payment. Gives false if it doesn't go through
    private bool PayByCard(DeliveryRequest job, decimal amountDue)
    {
        // Get the card number and expiry
        string cardNumber = ConsoleUI.Ask("Card number (16 digits)", InputValidator.CheckCardNumber);
        if (cardNumber == null)
        {
            return false;
        }

        string expiry = ConsoleUI.Ask("Expiry (MM/YY)", InputValidator.CheckExpiry);
        if (expiry == null)
        {
            return false;
        }

        // Remove spaces, then check before charging
        cardNumber = cardNumber.Replace(" ", "");
        if (!ConsoleUI.Confirm("Charge $" + amountDue.ToString("0.00") + " to the card ending " + cardNumber.Substring(12) + "?"))
        {
            ConsoleUI.ShowInfo("Card not charged.");
            return false;
        }

        // Charge the card
        Payment payment = _system.PayByCard(job.Id, _driver.Id, cardNumber);
        // Card was declined, so nothing was taken
        if (!payment.Approved)
        {
            ConsoleUI.ShowError("Card declined: " + payment.DeclineReason + ". No money was taken. Try another card, cash, or mark unpaid.");
            return false;
        }

        ConsoleUI.ShowSuccess("Payment approved.");
        ShowReceipt(payment, _driver.Name);
        return true;
    }

    // Take a cash payment and work out the change
    private bool PayByCash(DeliveryRequest job, decimal amountDue)
    {
        // Must be at least the amount owed
        string cash = ConsoleUI.Ask("Cash handed over (at least $" + amountDue.ToString("0.00") + ")", input => InputValidator.CheckMoney(input, amountDue, amountDue + 1000));
        if (cash == null)
        {
            return false;
        }

        decimal cashGiven = InputValidator.ParseNumber(cash);
        if (!ConsoleUI.Confirm("Received $" + cashGiven.ToString("0.00") + ". Give $" + (cashGiven - amountDue).ToString("0.00") + " change?"))
        {
            ConsoleUI.ShowInfo("Cash payment not recorded.");
            return false;
        }

        // Save the payment and show the receipt
        Payment payment = _system.PayByCash(job.Id, _driver.Id, cashGiven);
        ConsoleUI.ShowSuccess("Cash payment recorded.");
        ShowReceipt(payment, _driver.Name);
        return true;
    }

    // Mark the job not paid so pickup can still go ahead
    private bool MarkUnpaid(DeliveryRequest job)
    {
        string reason = ConsoleUI.Ask("Why can't the customer pay?", InputValidator.CheckRequired);
        if (reason == null || !ConsoleUI.Confirm("Mark the job unpaid and tell the office?"))
        {
            ConsoleUI.ShowInfo("Job not marked unpaid.");
            return false;
        }

        _system.MarkUnpaid(job.Id, _driver.Id, reason);
        ConsoleUI.ShowSuccess("Job marked unpaid. The office has been told.");
        return true;
    }

    // Record what happened when delivering a job
    private void DeliverJob()
    {
        ConsoleUI.ShowTitle("Deliver a Job");
        // Only jobs the driver is carrying
        List<DeliveryRequest> jobs = _system.GetDriverJobs(_driver.Id)
            .Where(j => j.Status == RequestStatus.PickedUp).ToList();
        if (jobs.Count == 0)
        {
            ConsoleUI.ShowInfo("You have no parcels waiting to be delivered.");
            ConsoleUI.Pause();
            return;
        }

        DeliveryRequest job = ConsoleUI.ChooseRequest("Reference of the job to deliver", jobs);
        if (job == null)
        {
            return;
        }

        ConsoleUI.ShowTitle("Deliver " + job.Id);
        ConsoleUI.ShowRequestDetails(job, _system);
        string[] options = { "Delivered - record who received it", "Recipient not home", "Recipient refused the parcels" };
        // Ask what happened at the door
        int choice = ConsoleUI.Choose("What happened?", options, "Back");

        // Delivered: save who got it
        if (choice == 1)
        {
            string receivedBy = ConsoleUI.Ask("Name of the person who received the parcels", InputValidator.CheckName);
            if (receivedBy != null && ConsoleUI.Confirm("Confirm delivery to " + receivedBy + "?"))
            {
                _system.ConfirmDelivery(job.Id, _driver.Id, receivedBy);
                ConsoleUI.ShowSuccess(job.Id + " delivered. The customer has been notified.");
                ConsoleUI.Pause();
            }
        }
        // Nobody home or they said no: record a failed delivery
        else if (choice == 2 || choice == 3)
        {
            string reason = choice == 2 ? "Recipient not home" : "Recipient refused the parcels";
            if (ConsoleUI.Confirm("Record failed delivery (" + reason + ")?"))
            {
                _system.ReportDeliveryFailed(job.Id, _driver.Id, reason);
                ConsoleUI.ShowSuccess("Failed delivery recorded. The office will arrange redelivery.");
                ConsoleUI.Pause();
            }
        }
    }
}
