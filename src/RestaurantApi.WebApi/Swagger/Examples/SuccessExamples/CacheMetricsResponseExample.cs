using RestaurantApi.Infrastructure.Cache;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class CacheMetricsResponseExample: IExamplesProvider<CacheMetricsStats>
{
    public CacheMetricsStats GetExamples()
    {
        return new CacheMetricsStats(Hit: 1240, Miss: 210, HitRatio: "85.52%");
    }
}
