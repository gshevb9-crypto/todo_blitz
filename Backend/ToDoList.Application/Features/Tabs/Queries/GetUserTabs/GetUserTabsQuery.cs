using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using MediatR;
using ToDoList.Application.Common.Interfaces;
using ToDoList.Application.Features.Tabs.DTOs;
using ToDoList.Domain.Entities;
using ToDoList.Domain.Repositories;

namespace ToDoList.Application.Features.Tabs.Queries.GetUserTabs
{
    public class GetUserTabsQuery : IRequest<ErrorOr<IReadOnlyList<TabDto>>>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetUserTabsQueryHandler : IRequestHandler<GetUserTabsQuery, ErrorOr<IReadOnlyList<TabDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserTabsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ErrorOr<IReadOnlyList<TabDto>>> Handle(GetUserTabsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Error.Unauthorized("auth.unauthorized", "المستخدم غير مسجل.");
            }

            var tabRepo = _unitOfWork.Repository<Tab, Guid>();
            var imageRepo = _unitOfWork.Repository<TabImage, Guid>();

            var tabs = await tabRepo.FindAsync(
                t => t.UserId == request.UserId && !t.IsDeleted,
                cancellationToken);

            var sortedTabs = tabs.OrderBy(t => t.OrderIndex).ThenBy(t => t.CreatedAt).ToList();

            var dtos = new List<TabDto>();
            foreach (var tab in sortedTabs)
            {
                var imageCount = await imageRepo.CountAsync(i => i.TabId == tab.Id && !i.IsDeleted, cancellationToken);
                dtos.Add(new TabDto
                {
                    Id = tab.Id,
                    Title = tab.Title,
                    Icon = tab.Icon,
                    OrderIndex = tab.OrderIndex,
                    CreatedAt = tab.CreatedAt,
                    UpdatedAt = tab.UpdatedAt,
                    ImagesCount = imageCount
                });
            }

            return dtos;
        }
    }
}
