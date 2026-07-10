using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;

public record ForgotPasswordVerifyCommand(string Email): IRequest<BaseResponse>;