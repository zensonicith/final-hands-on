using Handson.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Handson.Core.Providers
{
    public class ApiDataProvider : IDataProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;

        public ApiDataProvider(HttpClient httpClient, string url)
        {
            _httpClient = httpClient;
            _url = url;
        }

        public async Task<IEnumerable<T>> GetDataAsync<T>(CancellationToken token)
        {
            try
            {
                var response = await _httpClient.GetAsync(_url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return Enumerable.Empty<T>();
                }

                var jToken = JToken.Parse(jsonString);

                if (jToken is JObject obj && obj.ContainsKey("products"))
                {
                    return obj["products"].ToObject<IEnumerable<T>>() ?? [];
                }

                if (jToken is JArray array)
                {
                    return array.ToObject<IEnumerable<T>>() ?? [];
                }

                return Enumerable.Empty<T>();
            }
            catch(OperationCanceledException)
            {
                throw;
            }
        }
    }
}
