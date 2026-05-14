using RazorLight;
using RestaurantApi.Application.Mail;

namespace RestaurantApi.Infrastructure.Mail;

public class RazorTemplateRenderer: IMailTemplateRenderer
{
    private readonly RazorLightEngine _engine;

    public RazorTemplateRenderer()
    {
        var baseDir = AppContext.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "Mail", "Templates");

        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatePath)
            .SetOperatingAssembly(typeof(RazorTemplateRenderer).Assembly)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync<T>(string template, T model)
    {
        return await _engine.CompileRenderAsync(template, model);
    }
}