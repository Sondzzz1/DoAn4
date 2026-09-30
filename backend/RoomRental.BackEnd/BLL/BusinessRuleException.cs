namespace RoomRental.BackEnd.BLL;

public sealed class BusinessRuleException : Exception
{
    public int StatusCode { get; }

    public BusinessRuleException(string message, int statusCode = StatusCodes.Status400BadRequest)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public static BusinessRuleException NotFound(string message) =>
        new(message, StatusCodes.Status404NotFound);

    public static BusinessRuleException Forbidden(string message) =>
        new(message, StatusCodes.Status403Forbidden);

    public static BusinessRuleException Conflict(string message) =>
        new(message, StatusCodes.Status409Conflict);
}
