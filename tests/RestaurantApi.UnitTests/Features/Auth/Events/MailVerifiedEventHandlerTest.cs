using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.UnitTests.Features.Auth.Events;

public class MailVerifiedEventHandlerTest
{
    private readonly IMailFactory _mailFactoryMock;
    private readonly ICacheService _cacheServiceMock;
    private readonly ILogger<MailVerifiedEventHandler> _loggerMock;
    private readonly MailVerifiedEventHandler _handler;

    public MailVerifiedEventHandlerTest()
    {
        _mailFactoryMock = Substitute.For<IMailFactory>();
        _cacheServiceMock = Substitute.For<ICacheService>();
        _loggerMock = Substitute.For<ILogger<MailVerifiedEventHandler>>();

        _handler = new MailVerifiedEventHandler(_mailFactoryMock, _cacheServiceMock, _loggerMock);
    }

    [Fact]
    public async Task Handle_ShouldRemoveCacheAndSendWelcomeMail_WhenEventTriggered()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "john@gmail.com",
            Name = "John",
            Surname = "Doe"
        };

        var token = "mock-base64-token-123";
        var @event = new MailVerifiedEvent(user, token);
        var expectedCacheKey = CacheKeys.MailVerificationToken(token);

        await _handler.Handle(@event, CancellationToken.None);

        await _cacheServiceMock.Received(1).RemoveAsync(expectedCacheKey);
        await _mailFactoryMock.Received(1).SendAsync(
            MailTypes.Welcome.ToString(),
            user.Email,
            Arg.Is<WelcomeMailViewModel>(m =>
                m.CustomerEmail == user.Email &&
                m.CustomerName == "John Doe" &&
                m.RestaurantName == "Restaurant Api"
            )
        );
    }
}