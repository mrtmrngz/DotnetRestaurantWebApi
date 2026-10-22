using RestaurantApi.Application.Features.Cart.Commands.SetCartItemQuantityCommand;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.CartExample;

public class SetCartItemQuantityRequestExample: IExamplesProvider<SetCartItemQuantityCommand>
{
    public SetCartItemQuantityCommand GetExamples()
    {
        return new SetCartItemQuantityCommand(Quantity: 5);
    }
}