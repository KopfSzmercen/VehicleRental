namespace VehicleRental.Common.ErrorHandling;

public class BusinessRuleValidationException(string message) : Exception(message);