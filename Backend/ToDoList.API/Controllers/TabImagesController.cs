using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToDoList.API.Helpers;
using ToDoList.Application.Common.Models;
using ToDoList.Application.Features.Images.Commands.DeleteTabImage;
using ToDoList.Application.Features.Images.Commands.ReplaceTabImage;
using ToDoList.Application.Features.Images.Commands.UploadTabImage;
using ToDoList.Application.Features.Tabs.DTOs;

namespace ToDoList.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TabImagesController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly CurrentUser _currentUser;

        public TabImagesController(IMediator mediator, CurrentUser currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        [HttpPost("tabs/{tabId:guid}")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(Guid tabId, [FromForm] UploadImageForm form)
        {
            if (form?.File == null || form.File.Length == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("ملف الصورة مطلوب."));
            }

            var command = new UploadTabImageCommand
            {
                TabId = tabId,
                UserId = _currentUser.UserId!,
                File = form.File
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TabImageDto>.SuccessResponse(result.Value, "تم رفع الصورة وضغطها بنجاح."));
        }

        [HttpPut("{imageId:guid}")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> ReplaceImage(Guid imageId, [FromForm] UploadImageForm form)
        {
            if (form?.File == null || form.File.Length == 0)
            {
                return BadRequest(ApiResponse<object>.FailureResponse("ملف الصورة الجديد مطلوب."));
            }

            var command = new ReplaceTabImageCommand
            {
                ImageId = imageId,
                UserId = _currentUser.UserId!,
                File = form.File
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TabImageDto>.SuccessResponse(result.Value, "تم استبدال الصورة وحذف الملف القديم بنجاح."));
        }

        [HttpDelete("{imageId:guid}")]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            var command = new DeleteTabImageCommand
            {
                ImageId = imageId,
                UserId = _currentUser.UserId!
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "تم حذف الصورة والملف الفعلي بنجاح." }));
        }
    }

    public class UploadImageForm
    {
        public IFormFile File { get; set; } = null!;
    }
}
