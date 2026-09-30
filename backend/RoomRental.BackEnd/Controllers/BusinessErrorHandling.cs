using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Common;

namespace RoomRental.BackEnd.Controllers;

public sealed class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not BusinessRuleException exception) return;

        context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(exception.Message))
        {
            StatusCode = exception.StatusCode
        };
        context.ExceptionHandled = true;
    }
}

public static class BusinessErrorHandling
{
    public static IActionResult BusinessError<T>(this ControllerBase controller, Exception exception)
    {
        var statusCode = exception is BusinessRuleException business
            ? business.StatusCode
            : StatusCodes.Status400BadRequest;
        return controller.StatusCode(statusCode, ApiResponse<T>.ErrorResponse(exception.Message));
    }
}
