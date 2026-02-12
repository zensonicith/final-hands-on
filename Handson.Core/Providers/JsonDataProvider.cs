
using System.Net.Http.Json;
using System.Text.Json;

namespace Handson.Core.Providers
{
    public class JsonDataProvider : IDataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;

        public JsonDataProvider(HttpClient httpClient, string url)
        {
            _httpClient = httpClient;
            _url = url;
        }

        public async Task<IEnumerable<T>> GetDataAsync<T>()
        {
            var response = await _httpClient.GetAsync(_url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            // Kiểm tra nếu JSON là một Object có chứa property "products" (cho DummyJSON)
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("products", out var productsElement))
            {
                return JsonSerializer.Deserialize<IEnumerable<T>>(productsElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<T>();
            }

            // Nếu JSON là một Array thuần (cho các API khác như JSONPlaceholder)
            if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<IEnumerable<T>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<T>();
            }

            return Enumerable.Empty<T>();
        }
    }
}
