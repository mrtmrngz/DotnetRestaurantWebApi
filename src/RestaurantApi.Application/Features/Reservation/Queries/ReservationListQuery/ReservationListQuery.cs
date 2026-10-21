
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;

public record ReservationListQuery(
    Guid? UserId,
    ReservationListRequestEnums RequestType = ReservationListRequestEnums.CurrentUser
    ) : IRequest<GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>>;