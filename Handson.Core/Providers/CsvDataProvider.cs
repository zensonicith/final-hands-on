using System.Globalization;
using CsvHelper;
using Handson.Core.Settings;
using Microsoft.Extensions.Options;

namespace Handson.Core.Providers
{
    public class CsvDataProvider : IDataProvider
    {
        private readonly string _filePath;
        public CsvDataProvider(IOptions<StorageSettings> options)
        {
            string storagePath = options.Value.StoragePath;
            string fileName = options.Value.FileName.First();
            _filePath = Path.Combine(storagePath, fileName);
        }

        public async Task<IEnumerable<T>> GetDataAsync<T>(CancellationToken token)
        {
            try
            {
                if (!File.Exists(_filePath))
                    throw new FileNotFoundException($"File not found: {_filePath}");

                List<T> records = new List<T>();
                using var reader = new StreamReader(_filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<T>(token))
                {
                    records.Add(record);
                }
                return records;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
    }
}
