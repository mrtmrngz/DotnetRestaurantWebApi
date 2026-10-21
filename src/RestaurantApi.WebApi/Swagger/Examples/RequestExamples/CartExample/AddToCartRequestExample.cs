using RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.CartExample;

public class AddToCartRequestExample: IExamplesProvider<AddToCartCommand>
{
    public AddToCartCommand GetExamples()
    {
        return new AddToCartCommand(
            ProductId: Guid.NewGuid(),
            Quantity: 2
        );
    }
}