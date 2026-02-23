using Handson.Core.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
            string json = await File.ReadAllTextAsync(_filePath, token);
            var jToken = JToken.Parse(json);

            if (jToken is JObject obj)
            {
                var arrayValue = obj.Properties().FirstOrDefault(
                    p => p.Value.Type == JTokenType.Array
                )?.Value;

                return arrayValue?.ToObject<List<T>>() ?? new List<T>();
            }

            return jToken.ToObject<List<T>>() ?? new List<T>();

        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    private static bool HasRequiredFields<T>(T record)
    {
        if (record == null) return false;

        return record.GetType().GetProperties().All(p => p.GetValue(record) != null);

    }
}
