using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.DTO.Common;

namespace RoomRental.BackEnd.Controllers;

public sealed class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, message) = BusinessErrorHandling.Describe(context.Exception, context.HttpContext);
        context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(message))
        {
            StatusCode = status
        };
        context.ExceptionHandled = true;
    }
}

public static class BusinessErrorHandling
{
    public const string UnexpectedMessage = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

    internal static (int Status, string Message) Describe(Exception exception, HttpContext context)
    {
        if (exception is BusinessRuleException business) return (business.StatusCode, business.Message);
        context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("ApiErrors")
            .LogError(exception, "Unexpected API error at {Path}", context.Request.Path);
        return (StatusCodes.Status500InternalServerError, UnexpectedMessage);
    }

    public static IActionResult BusinessError<T>(this ControllerBase controller, Exception exception)
    {
        var (status, message) = Describe(exception, controller.HttpContext);
        return controller.StatusCode(status, ApiResponse<T>.ErrorResponse(message));
    }
}
