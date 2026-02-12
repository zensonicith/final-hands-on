using System.Globalization;
using CsvHelper;

namespace Handson.Core.Providers
{
    public class CsvDataProvider : IDataProvider
    {
        private readonly string _filePath;
        public CsvDataProvider(string filePath)
        {
            _filePath = filePath;
        }
        public async Task<IEnumerable<T>> GetDataAsync<T>()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"File not found: {_filePath}");

            List<T> records = new List<T>();
            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            await foreach (var record in csv.GetRecordsAsync<T>())
            {
                records.Add(record);
            }
            return records;
        }
    }
}
