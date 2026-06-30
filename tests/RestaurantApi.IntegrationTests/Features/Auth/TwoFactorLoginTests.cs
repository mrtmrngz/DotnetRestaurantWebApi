using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Auth.Commands.TwoFactorLogin;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class TwoFactorCreateUserAndTokenDto
{
    public AppUser User { get; set; } = null!;
    public string Otp { get; set; } = null!;
}

public class TwoFactorLoginTests: BaseIntegrationTest
{
    public TwoFactorLoginTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // Success TESTS START

    [Fact]
    public async Task TwoFactorLogin_WithValidOtp_ShouldReturnSuccessResponse()
    {
        var data = await CreateUser();

        using var scope = Factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var validOtp = await userRepository.GenerateTwoFactorTokenAsync(data.User);

        var cacheKey = CacheKeys.OtpToken(validOtp, "twoFactorAuth");
        await cacheService.SetAsync(cacheKey, data.User.Id.ToString(), TimeSpan.FromMinutes(3));

        var command = new TwoFactorLoginCommand(validOtp);

        var response = await mediator.Send(command);

        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.Code.Should().Be(Codes.LOGIN_SUCCESS);

        var cachedUserId = await cacheService.GetAsync<string>(cacheKey);
        cachedUserId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task TwoFactorLoginApi_WithValidOtp_ShouldReturnSuccessResponse()
    {
        var data = await CreateUser();
        var command = new TwoFactorLoginCommand(Otp: data.Otp);

        var response = await Client.PostAsJsonAsync("/api/auth/two-factor/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNull();
        result.Code.Should().Be(Codes.LOGIN_SUCCESS);

        var setCookieHeader = response.Headers.GetValues("Set-Cookie");
        setCookieHeader.Should().NotBeNull("Giriş başarılı olduğunda Set-Cookie header'ı dönmeliydi.");

        var sessionCookie = setCookieHeader.FirstOrDefault(c => c.StartsWith("_session"));
        sessionCookie.Should().NotBeNull("Dönen cookie'lerin arasında '_session' adında bir cookie bulunmalı.");

        var rawCookieValue = sessionCookie!.Split(';')[0];
        var refreshTokenFromCookie = rawCookieValue.Split('=')[1];

        refreshTokenFromCookie.Should().NotBeNullOrEmpty("Cookie içerisindeki refresh token boş olamaz.");
        sessionCookie.Should().Contain("httponly", "Güvenlik gereği refresh token cookie'si HttpOnly olmalıdır!");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var otp = await cacheService.GetAsync<string>(CacheKeys.OtpToken(data.Otp, "twoFactorAuth"));
            otp.Should().BeNull("Redisten otp temizleneceği için sonuç null olmalı.");
        }
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("abcd12")]
    public async Task TwoFactorLoginApi_WithInvalidOtpFormat_ShouldReturnBadRequestWithValidationErrors(string invalidOtp)
    {
        var command = new TwoFactorLoginCommand(Otp: invalidOtp);

        var response = await Client.PostAsJsonAsync("/api/auth/two-factor/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);

        result.Should().NotBeNull();
        result.Code.Should().Be(Codes.VALIDATION_ERROR);
    }
    
    // Success TESTS END
    
    // ERROR TESTS START

    [Fact]
    public async Task TwoFactorLogin_WithInvalidOtp_ShouldReturnUnauthorizedException()
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new TwoFactorLoginCommand(Otp: "123456");

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("İki faktörlü kimlik doğrulama oturum süresi sona erdi, lütfen tekrar giriş yapınız.");
    }

    [Fact]
    public async Task TwoFactorLogin_WithValidOtpButNotExistUser_ShouldThrowUnauthorizedException()
    {
        var data = await CreateUser();
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        context.Users.Remove(data.User);
        await context.SaveChangesAsync();
        
        var command = new TwoFactorLoginCommand(Otp: data.Otp);

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Kimlik doğrulama oturumu geçersiz veya süresi dolmuş.");
    }

    [Fact]
    public async Task TwoFactorLogin_WhenUserLockout_ShouldThrowBadRequestException()
    {
        var data = await CreateUser(u =>
        {
            u.LockoutEnabled = true;
            u.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
        });
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new TwoFactorLoginCommand(Otp: data.Otp);
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Çok fazla hatalı deneme nedeniyle hesabınız geçici olarak kilitlenmiştir.");
    }

    [Fact]
    public async Task TwoFactorLogin_WhenUserLockoutAfter5thFailedAccessAttempt_ShouldThrowBadRequestException()
    {
        var data = await CreateUser(u =>
        {
            u.AccessFailedCount = 4;
            u.LockoutEnabled = true;
        }, "999999");
        
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new TwoFactorLoginCommand(Otp: data.Otp);
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Çok fazla hatalı deneme nedeniyle hesabınız geçici olarak kilitlenmiştir.");

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var user = await context.Users.FindAsync(data.User.Id);

        user.Should().NotBeNull("Kullanıcı bulunması gerekir.");
        user.LockoutEnabled.Should().BeTrue("Kullanıcı kilitli olması gerekir.");           
        user.AccessFailedCount.Should().Be(0, "Kullanıcının hatalı giriş sayısı sıfırlanmış olması gerekir.");
        user.LockoutEnd.Should().BeAfter(DateTime.UtcNow);
    }
    
    [Fact]
    public async Task TwoFactorLogin_WhenInvalidOtp_ShouldFailedAccessCountIncreaseAndThrowBadRequestException()
    {
        var data = await CreateUser(u =>
        {
            u.AccessFailedCount = 2;
            u.LockoutEnabled = false;
        }, "999999");
        
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new TwoFactorLoginCommand(Otp: data.Otp);
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Girdiğiniz doğrulama kodu hatalı veya süresi dolmuş.");

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var user = await context.Users.FindAsync(data.User.Id);

        user.Should().NotBeNull("Kullanıcı bulunması gerekir.");
        user.LockoutEnabled.Should().BeFalse("Kullanıcı kilitli olmaması gerekir.");           
        user.AccessFailedCount.Should().Be(3, "Kullanıcının hatalı giriş sayısı sıfırlanmış olması gerekir.");
    }
    
    // ERROR TESTS END

    private async Task<TwoFactorCreateUserAndTokenDto> CreateUser(Action<AppUser>? customConfig = null, string? customOtp = null)
    {
        var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        var email = "test@mail.com";
        var password = "Test123";
        
        var user = new AppUser
        {
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            Name = "John",
            Surname = "Doe",
            EmailConfirmed = true,
            TwoFactorEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D")
        };
        
        customConfig?.Invoke(user);

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var otp = customOtp ?? await userRepo.GenerateTwoFactorTokenAsync(user);

        var cacheKey = CacheKeys.OtpToken(otp, "twoFactorAuth");
        await cacheService.SetAsync(cacheKey, user.Id.ToString(), TimeSpan.FromMinutes(5));

        return new TwoFactorCreateUserAndTokenDto{User = user, Otp = otp};
    }
}