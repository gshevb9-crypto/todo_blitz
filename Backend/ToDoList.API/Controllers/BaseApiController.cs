using System.Collections.Generic;
using System.Linq;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToDoList.Application.Common.Models;

namespace ToDoList.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult HandleErrorResult(IReadOnlyList<Error> errors)
        {
            var firstError = errors.FirstOrDefault();
            var response = ApiResponse<object>.FailureResponse(firstError.Description);

            return firstError.Type switch
            {
                ErrorType.NotFound => NotFound(response),
                ErrorType.Validation => BadRequest(response),
                ErrorType.Conflict => Conflict(response),
                ErrorType.Unauthorized => Unauthorized(response),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, response),
                ErrorType.Failure => BadRequest(response),
                _ => StatusCode(StatusCodes.Status500InternalServerError, response)
            };
        }
    }
}
