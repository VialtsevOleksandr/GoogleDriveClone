using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using Microsoft.AspNetCore.Mvc;

namespace GoogleDriveClone.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "Запит виконано успішно")
    {
        if (!result.IsSuccess) return HandleError(result.Error!);
        
        var response = new ApiResponse<T>(true, successMessage, result.Value!);
        return Ok(response);
    }

    protected IActionResult HandleResult(Result result, string successMessage = "Запит виконано успішно")
    {
        if (!result.IsSuccess) return HandleError(result.Error!);

        var response = new ApiResponse(true, successMessage);
        return Ok(response);
    }

    private IActionResult HandleError(Error error)
    {
        var errorResponse = new ApiErrorResponse(false, error);
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(errorResponse),
            ErrorType.Unauthorized => Unauthorized(errorResponse),
            ErrorType.Conflict => Conflict(errorResponse),
            ErrorType.Validation => BadRequest(errorResponse),
            _ => BadRequest(errorResponse)
        };
    }
}