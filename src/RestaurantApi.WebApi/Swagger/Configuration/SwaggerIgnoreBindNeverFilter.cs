using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

public class SwaggerIgnoreBindNeverFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content == null) return;

        foreach (var content in operation.RequestBody.Content.Values)
        {
            if (content.Schema?.Properties == null) continue;

            var ignoredProperties = context.MethodInfo.GetParameters()
                .SelectMany(p => p.ParameterType.GetProperties())
                .Where(p => p.GetCustomAttributes(typeof(BindNeverAttribute), true).Any())
                .Select(p => p.Name)
                .ToList();

            foreach (var propName in ignoredProperties)
            {
                var keyToRemove = content.Schema.Properties.Keys
                    .FirstOrDefault(k => k.Equals(propName, StringComparison.OrdinalIgnoreCase));

                if (keyToRemove != null)
                {
                    content.Schema.Properties.Remove(keyToRemove);
                }
            }
        }
    }
}