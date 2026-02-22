using Handson.Core;
using Handson.Core.Models;
using Handson.Core.Providers;
using Handson.Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

// Setup Configuration & Logging
IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

ServiceCollection services = new ServiceCollection(); // Initialize DI Container

services.AddHttpClient();

services.AddLogging(builder =>
{
    builder.AddConfiguration(config.GetSection("Logging")); // Read Logging settings from appsettings.json
    builder.AddConsole(); // Print logs to Console
});

// DI Logic - Register Provider

services.Configure<ProviderOptions>(config.GetSection("ProviderOptions"));
services.Configure<StorageSettings>(config.GetSection("StorageSettings"));

services.AddScoped<ApiDataProvider>();
services.AddScoped<CsvDataProvider>();
services.AddScoped<JsonDataProvider>();
services.AddScoped<IProviderFactory, ProviderFactory>();

services.AddScoped<IDataProvider>(sp => sp.GetRequiredService<IProviderFactory>().Create());

ServiceProvider serviceProvider = services.BuildServiceProvider();
ILogger logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Main Flow với Global Exception Handling
while (true)
{
    using (var scope = serviceProvider.CreateScope())
    {
        var provider = scope.ServiceProvider;

        var providerOptions = provider.GetRequiredService<IOptionsMonitor<ProviderOptions>>();
        var storageSettings = provider.GetRequiredService<IOptionsMonitor<StorageSettings>>();
        var dataProvider = provider.GetRequiredService<IDataProvider>();

        const string URL = "ApiUrl";

        var startTime = DateTime.Now;
        var sw = Stopwatch.StartNew();

        logger.LogInformation(">>> Application Started: Mode {Mode} <<<", providerOptions.CurrentValue);
        logger.LogInformation("[Starting: {StartTime}]", startTime);
        Console.WriteLine("--------------------------------------------------");

        try
        {
            CancellationTokenSource cts = new();

            if (providerOptions.CurrentValue.Equals("CSV"))
            {
                var data = await dataProvider.GetDataAsync<HousePrice>(cts.Token);

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
            else if (providerOptions.CurrentValue.Equals("API"))
            {
                var data = await dataProvider.GetDataAsync<Product>(cts.Token);

                List<Product> premiumProducts = data
                    .Where(p => p.Price > 500 && p.Rating >= 4.0)
                    .OrderByDescending(p => p.Price)
                    .ToList();

                logger.LogInformation("Fetched {Count} products from API.", data.Count());
                Console.WriteLine("\n--- TOP PREMIUM PRODUCTS ---");
                foreach (var p in premiumProducts)
                    Console.WriteLine($"[{p.Category}] {p.Title,-20} | Price: ${p.Price,-8} | Rating: {p.Rating}");
            }

            sw.Stop();
            var endTime = DateTime.Now;
            logger.LogInformation($"[Ending: {endTime:HH:mm:ss:fff}]");
            logger.LogInformation(">>> Processing Completed Successfully <<<");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation is cancelled !");
        }
        catch (Exception ex)
        {
            // Global Exception Handling Requirement
            logger.LogCritical(ex, "An unhandled exception occurred during execution.");
            throw;
        }
    }
}