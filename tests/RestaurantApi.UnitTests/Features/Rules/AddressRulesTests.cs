using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;
using RestaurantApi.Application.Features.Rules.AddressRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class AddressRulesTests
{
    private readonly ILogger<AddressRules> _loggerMock = Substitute.For<ILogger<AddressRules>>();
    private readonly AddressRules _sut;

    public AddressRulesTests()
    {
        _sut = new AddressRules(_loggerMock);
    }

    #region ShouldAddressExist TESTS

    [Fact]
    public async Task ShouldAddressExist_WhenAddressNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Address? add = null;

        var act = async () => await _sut.ShouldAddressExist(add, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Adres bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldAddressExist_WhenAddressExist_ShouldNotThrowAsync()
    {
        Domain.Entities.Address add = new Domain.Entities.Address{ Id = Guid.NewGuid()};

        var act = async () => await _sut.ShouldAddressExist(add, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    #endregion
    
    #region ShouldAddressExistInCache TESTS

    [Fact]
    public async Task ShouldAddressExistInCache_WhenAddressNotExistInCache_ShouldThrowNotFoundException()
    {
        GetUserAddressQueryResult? add = null;

        var act = async () => await _sut.ShouldAddressExistInCache(add, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Adres bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldAddressExistInCache_WhenAddressExistInCache_ShouldNotThrowAsync()
    {
        GetUserAddressQueryResult add = new GetUserAddressQueryResult(
            Id: Guid.Parse("019f81c8-1fc2-77b2-b691-2e6eb320461a"),
            Title: "Yazlık / Çiftlik",
            RecipientName: "Mert Marangoz",
            City: "Aydın",
            Town: "Söke",
            Neighborhood: "Yenidoğan Mahallesi",
            Street: "Atatürk Caddesi",
            BuildingInfo: null,
            BuildingNumber: "45",
            PhoneNumber: "+905321234567",
            IsDefault: false,
            ZipCode: "09200"
        );

        var act = async () => await _sut.ShouldAddressExistInCache(add, Guid.NewGuid(), Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    #endregion
}