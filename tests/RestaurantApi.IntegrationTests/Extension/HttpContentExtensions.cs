using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantApi.IntegrationTests.Extension;

public static class HttpContentExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<T?> ReadContentAsAsync<T>(this HttpResponseMessage response)
    {
        if (response.Content == null) return default;
        
        return await response.Content.ReadFromJsonAsync<T>(DefaultOptions);
    }
}