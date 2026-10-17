using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;

namespace RestaurantApi.UnitTests.Features.RestaurantTable.Queries;

public class GetRestaurantTableDetailQueryValidatorTests
{
    private readonly GetRestaurantTableDetailQueryValidator _validator;

    public GetRestaurantTableDetailQueryValidatorTests()
    {
        _validator = new GetRestaurantTableDetailQueryValidator();
    }

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenTableIdIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var query = new GetRestaurantTableDetailQuery(Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TableId Tests

    [Fact]
    public void Validate_WhenTableIdIsEmpty_ShouldHaveValidationError()
    {
        var query = new GetRestaurantTableDetailQuery(Guid.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Masa id boş olamaz.");
    }

    #endregion
}
