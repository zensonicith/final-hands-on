using Handson.Core.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Handson.Core.Providers;

public class JsonDataProvider : IDataProvider
{
    private readonly string _filePath;
    public JsonDataProvider(IOptions<StorageSettings> options)
    {
        string storagePath = options.Value.StoragePath;
        string fileName = options.Value.FileName.Last();
        _filePath = Path.Combine(storagePath, fileName);
    }

    public async Task<IEnumerable<T>> GetDataAsync<T>(CancellationToken token)
    {
        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"File not found: {_filePath}");

        try
        {
            using var reader = new StreamReader(_filePath);
            string body = await reader.ReadToEndAsync(token);

            var jToken = JToken.Parse(body);

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
        catch (OperationCanceledException)
        {
            throw;
        }
    }
}
