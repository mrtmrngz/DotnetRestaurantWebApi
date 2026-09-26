using Microsoft.Extensions.Logging;

namespace RestaurantApi.Application.Features.Rules.ProductRules;

public class ProductRules
{
    private readonly ILogger<ProductRules> _logger;

    public ProductRules(ILogger<ProductRules> logger)
    {
        _logger = logger;
    }
}