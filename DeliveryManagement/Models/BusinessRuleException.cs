namespace DeliveryManagement.Models;

// Error for when a rule is broken. The screens catch it and show the message
public class BusinessRuleException : Exception
{
    // Make the error with no message
    public BusinessRuleException()
    {
    }

    // Make the error with a message
    public BusinessRuleException(string message)
        : base(message)
    {
    }

    // Make the error with a message and the error that caused it
    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
