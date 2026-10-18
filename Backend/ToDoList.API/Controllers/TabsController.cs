using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToDoList.API.Helpers;
using ToDoList.Application.Common.Models;
using ToDoList.Application.Features.Tabs.Commands.CreateTab;
using ToDoList.Application.Features.Tabs.Commands.DeleteTab;
using ToDoList.Application.Features.Tabs.Commands.UpdateTab;
using ToDoList.Application.Features.Tabs.Commands.UpdateTabContent;
using ToDoList.Application.Features.Tabs.DTOs;
using ToDoList.Application.Features.Tabs.Queries.GetTabById;
using ToDoList.Application.Features.Tabs.Queries.GetUserTabs;

namespace ToDoList.API.Controllers
{
    [Authorize]
    public class TabsController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly CurrentUser _currentUser;

        public TabsController(IMediator mediator, CurrentUser currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetTabs()
        {
            var query = new GetUserTabsQuery
            {
                UserId = _currentUser.UserId!
            };

            var result = await _mediator.Send(query);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<IReadOnlyList<TabDto>>.SuccessResponse(result.Value));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTabById(Guid id)
        {
            var query = new GetTabByIdQuery
            {
                Id = id,
                UserId = _currentUser.UserId!
            };

            var result = await _mediator.Send(query);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TabDetailDto>.SuccessResponse(result.Value));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTab([FromBody] CreateTabRequest request)
        {
            var command = new CreateTabCommand
            {
                UserId = _currentUser.UserId!,
                Title = request.Title,
                Icon = request.Icon,
                OrderIndex = request.OrderIndex
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return CreatedAtAction(nameof(GetTabById), new { id = result.Value.Id }, ApiResponse<TabDto>.SuccessResponse(result.Value));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTab(Guid id, [FromBody] UpdateTabRequest request)
        {
            var command = new UpdateTabCommand
            {
                Id = id,
                UserId = _currentUser.UserId!,
                Title = request.Title,
                Icon = request.Icon,
                OrderIndex = request.OrderIndex
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TabDto>.SuccessResponse(result.Value, "تم تحديث التاب بنجاح."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTab(Guid id)
        {
            var command = new DeleteTabCommand
            {
                Id = id,
                UserId = _currentUser.UserId!
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "تم حذف التاب ومحتوياته بنجاح." }));
        }

        [HttpPut("{id:guid}/content")]
        public async Task<IActionResult> UpdateTabContent(Guid id, [FromBody] UpdateContentRequest request)
        {
            var command = new UpdateTabContentCommand
            {
                TabId = id,
                UserId = _currentUser.UserId!,
                TextContent = request.TextContent ?? string.Empty
            };

            var result = await _mediator.Send(command);
            if (result.IsError)
            {
                return HandleErrorResult(result.Errors);
            }

            return Ok(ApiResponse<TabContentDto>.SuccessResponse(result.Value, "تم حفظ المحتوى بنجاح."));
        }
    }

    public class CreateTabRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdateTabRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int? OrderIndex { get; set; }
    }

    public class UpdateContentRequest
    {
        public string? TextContent { get; set; }
    }
}
