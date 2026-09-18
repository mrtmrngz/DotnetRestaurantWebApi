using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.MediaRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class MediaRulesTests
{
    private readonly ILogger<MediaRules> _loggerMock = Substitute.For<ILogger<MediaRules>>();
    private readonly MediaRules _sut;

    public MediaRulesTests()
    {
        _sut = new MediaRules(_loggerMock);
    }

    #region ShouldMediaExist Tests

    [Fact]
    public async Task ShouldMediaExist_WhenMediaNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Media? detail = null;

        var act = async () => await _sut.ShouldMediaExist(detail);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("İlgili medya bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldMediaExist_WhenMediaExist_ShouldNotThrowAnything()
    {
        Domain.Entities.Media? detail = new Domain.Entities.Media();

        var act = async () => await _sut.ShouldMediaExist(detail);

        await act.Should().NotThrowAsync();
    }
    

    #endregion
}