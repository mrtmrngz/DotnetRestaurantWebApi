using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;
using RestaurantApi.Application.Features.Rules.CategoryRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class CategoryRulesTests
{
    private readonly ILogger<CategoryRules> _loggerMock = Substitute.For<ILogger<CategoryRules>>();
    private readonly CategoryRules _sut;

    public CategoryRulesTests()
    {
        _sut = new CategoryRules(_loggerMock);
    }

    #region ShouldCategoryDetailExist Tests

    [Fact]
    public async Task ShouldCategoryDetailExist_WhenCategoryDetailNotExist_ShouldThrowNotFoundException()
    {
        CategoryDetailQueryResult? detail = null;

        var act = async () => await _sut.ShouldCategoryDetailExist(detail);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Kategori bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldCategoryDetailExist_WhenCategoryDetailExist_ShouldNotThrowAnything()
    {
        CategoryDetailQueryResult? detail = new CategoryDetailQueryResult() with
        {
            Id = Guid.NewGuid(),
            Title = "Title",
            Slug = "title",
            IsDeleted = false,
            ImageUrl = "img.url"
        }; 

        var act = async () => await _sut.ShouldCategoryDetailExist(detail);

        await act.Should().NotThrowAsync();
    }

    #endregion
    
    #region ShouldCategoryEntityExist Tests

    [Fact]
    public async Task ShouldCategoryEntityExist_WhenCategoryEntityNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Category? detail = null;

        var act = async () => await _sut.ShouldCategoryEntityExist(detail);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Kategori bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldCategoryEntityExist_WhenCategoryEntityExist_ShouldNotThrowAnything()
    {
        Domain.Entities.Category? detail = new Domain.Entities.Category();

        var act = async () => await _sut.ShouldCategoryEntityExist(detail);

        await act.Should().NotThrowAsync();
    }

    #endregion
    
    #region ShouldBeValidForDeletion Tests

    [Fact]
    public async Task ShouldBeValidForDeletion_WhenCategoryNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Category? detail = null;

        // Act
        Action act = () => _sut.ShouldBeValidForDeletion(detail);

        // Assert
        act.Should().Throw<NotFoundException>()
            .WithMessage("Kategori bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldBeValidForDeletion_WhenCategoryAlreadyDeleted_ShouldThrowNotFoundException()
    {
        Domain.Entities.Category? detail = new Domain.Entities.Category(){IsDeleted = true};

        // Act
        Action act = () => _sut.ShouldBeValidForDeletion(detail);

        // Assert
        act.Should().Throw<NotFoundException>()
            .WithMessage("Kategori bulunamadı.");
    }
    
    [Fact]
    public async Task ShouldBeValidForDeletion_WhenCategoryCanDeleted_ShouldNotThrowAnything()
    {
        Domain.Entities.Category? detail = new Domain.Entities.Category{IsDeleted = false};

        // Act
        Action act = () => _sut.ShouldBeValidForDeletion(detail);

        // Assert
        act.Should().NotThrow();
    }

    #endregion
    
    #region ShouldNotHasAnyActiveProduct Tests

    [Fact]
    public async Task ShouldNotHasAnyActiveProduct_WhenCategoryHasActiveProduct_ShouldThrowUnprocessableEntityError()
    {
        // Act
        Action act = () => _sut.ShouldNotHasAnyActiveProduct(true, Guid.NewGuid());

        // Assert
        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("Bu kategoriye bağlı aktif ürünler bulunmaktadır. Lütfen önce ürünleri silin veya başka bir kategoriye taşıyın.");
    }
    
    
    [Fact]
    public async Task ShouldNotHasAnyActiveProduct_WhenCategoryHasNotActiveProduct_ShouldNotThrowAnything()
    {
        // Act
        Action act = () => _sut.ShouldNotHasAnyActiveProduct(false, Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}