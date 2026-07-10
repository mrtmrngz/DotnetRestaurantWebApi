using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.UnitTests.Features.Auth.Events;

public class ForgotPasswordVerifyEventHandlerTests
{
    private readonly ILogger<ForgotPasswordVerifyEventHandler> _loggerMock;
    private readonly ICacheService _cacheServiceMock;
    private readonly IMailFactory _mailFactoryMock;
    private readonly ForgotPasswordVerifyEventHandler _handler;

    public ForgotPasswordVerifyEventHandlerTests()
    {
        _loggerMock = Substitute.For<ILogger<ForgotPasswordVerifyEventHandler>>();
        _mailFactoryMock = Substitute.For<IMailFactory>();
        _cacheServiceMock = Substitute.For<ICacheService>();

        _handler = new ForgotPasswordVerifyEventHandler(_loggerMock, _cacheServiceMock, _mailFactoryMock);
    }

    [Fact]
    public async Task Handle_WhenEventTriggerd_ShouldSaveTokenCacheAndSendEmail()
    {
        var rawToken = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var email = "test@mail.com";
        var @event =
            new ForgotPasswordVerifyEvent(userId, rawToken, email, "John Doe", 15);
        var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(rawToken)
        );
        var exceptedCacheKey = CacheKeys.ForgotPasswordKey(encodedToken);

        await _handler.Handle(@event, CancellationToken.None);

        await _cacheServiceMock.Received(1).SetAsync(exceptedCacheKey, userId, TimeSpan.FromMinutes(15));

        await _mailFactoryMock.Received(1).SendAsync(
            MailTypes.ForgotPassword.ToString(),
            email,
            Arg.Is<ForgotPasswordViewModel>(m =>
                m.CustomerEmail == email &&
                m.CustomerName == "John Doe" &&
                m.RestaurantName == "Restaurant API" &&
                m.Title == "Parola değiştirme isteği."
            )
        );
    }
}