using Cis.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Common;

internal static class PaginationResponseExtensions
{
    public static ActionResult<ApiResponse<IReadOnlyCollection<T>>> OkPaged<T>(
        this ControllerBase controller,
        IReadOnlyCollection<T> items,
        PaginationRequest pagination)
    {
        var totalCount = items.Count;
        var pageItems = items
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToArray();

        return controller.Ok(ApiResponse<IReadOnlyCollection<T>>.Success(
            pageItems,
            controller.HttpContext.TraceIdentifier,
            PaginationMeta.Create(pagination.PageNumber, pagination.PageSize, totalCount)));
    }

    public static ActionResult<ApiResponse<IReadOnlyCollection<T>>> OkPaged<T>(
        this ControllerBase controller,
        PagedResult<T> result)
    {
        return controller.Ok(ApiResponse<IReadOnlyCollection<T>>.Success(
            result.Items,
            controller.HttpContext.TraceIdentifier,
            result.ToPaginationMeta()));
    }
}
