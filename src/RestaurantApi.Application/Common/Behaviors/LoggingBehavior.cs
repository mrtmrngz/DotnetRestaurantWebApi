using MediatR;
using Microsoft.Extensions.Logging;

namespace RestaurantApi.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{

    private ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        
        _logger.LogInformation("➡️ Handling {Request}", typeof(TRequest).Name);

        var response = await next();

        var elapsed = DateTime.UtcNow - start;
        
        _logger.LogInformation(
            "✅ Handled {Request} in {Ms}ms",
            typeof(TRequest).Name,
            elapsed.TotalMilliseconds
        );

        return response;
    }
}