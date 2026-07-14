using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordApply;

public record ForgotPasswordApplyCommand(string Token, string Password): IRequest<BaseResponse>;