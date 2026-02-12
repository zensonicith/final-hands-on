using Handson.Core.Models;
using Handson.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

// Setup Configuration & Logging
IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

ServiceCollection serviceCollection = new ServiceCollection(); // Initialize DI Container

serviceCollection.AddLogging(builder =>
{
    builder.AddConfiguration(config.GetSection("Logging")); // Read Logging settings from appsettings.json
    builder.AddConsole(); // Print logs to Console
});

serviceCollection.AddHttpClient();

// DI Logic - Register Provider
const string API_URL = "ProviderConfig:ApiUrl";
const string CSV_PATH = "ProviderConfig:CsvPath";
const string PROVIDER_TYPE = "ProviderConfig:Type";

var sourceType = config[PROVIDER_TYPE];

if (string.Equals(sourceType, "API", StringComparison.OrdinalIgnoreCase))
{
    serviceCollection.AddSingleton<IDataProvider>(
        sp => new JsonDataProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
            config[API_URL] ?? "https://dummyjson.com/products"
        )
     );
}
else
{
    serviceCollection.AddSingleton<IDataProvider>(
        new CsvDataProvider(config[CSV_PATH] ?? "C:\\Users\\son.nguyen2\\Desktop\\LearningSpace\\dotnet\\Handson.ConsoleApp\\Handson.Core\\_data\\House_Prices.csv")
    );
}

ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
ILogger logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Main Flow với Global Exception Handling
try
{
    logger.LogInformation(">>> Application Started: Mode {Mode} <<<", sourceType);

    if (sourceType == "API")
    {
        IDataProvider provider = serviceProvider.GetRequiredService<IDataProvider>();
        var data = await provider.GetDataAsync<Product>();

        // Lọc sản phẩm đắt tiền và Rating cao
        List<Product> premiumProducts = data
            .Where(p => p.Price > 500 && p.Rating >= 4.0)
            .OrderByDescending(p => p.Price)
            .ToList();

        logger.LogInformation("Fetched {Count} products from API.", data.Count());
        Console.WriteLine("\n--- TOP PREMIUM PRODUCTS ---");
        foreach (var p in premiumProducts)
            Console.WriteLine($"[{p.Category}] {p.Title,-20} | Price: ${p.Price,-8} | Rating: {p.Rating}");
    }
    else
    {
        IDataProvider provider = serviceProvider.GetRequiredService<IDataProvider>();
        var data = await provider.GetDataAsync<HousePrice>();

        // Thống kê giá theo tình trạng nội thất (Requirement: Group, Count, Avg)
        var summary = data
            .GroupBy(h => h.FurnishingStatus)
            .Select(g => new
            {
                Status = g.Key ?? "Unknown",
                Count = g.Count(),
                AvgPrice = g.Average(h => h.Price),
                MaxPrice = g.Max(h => h.Price)
            })
            .OrderBy(x => x.Count)
            .ToList();

        logger.LogInformation("Read {Count} rows from CSV file.", data.Count());
        Console.WriteLine("\n--- HOUSE PRICE SUMMARY BY FURNISHING ---");
        Console.WriteLine($"{"Status",-20} | {"Count",-10} | {"Avg Price (USD)",-15} | {"Max Price",-15}");
        Console.WriteLine(new string('-', 65));

        foreach (var item in summary)
        {
            Console.WriteLine($"{item.Status,-20} | {item.Count,-10} | {item.AvgPrice:N0} | {item.MaxPrice,15:N0}");
        }
    }

    logger.LogInformation(">>> Processing Completed Successfully <<<");
}
catch (Exception ex)
{
    // Global Exception Handling Requirement
    logger.LogCritical(ex, "An unhandled exception occurred during execution.");
}