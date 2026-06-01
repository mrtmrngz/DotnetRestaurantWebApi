using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.MailVerify;

public record MailVerifyCommand(string Token): IRequest<BaseResponse>;