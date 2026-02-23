using Handson.Core;
using Handson.Core.Models;
using Handson.Core.Providers;
using Handson.Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Options;
using System.Diagnostics;

// Setup Configuration & Logging
IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

ServiceCollection services = new ServiceCollection(); // Initialize DI Container

services.AddHttpClient();

services.Configure<ProviderOptions>(config.GetSection("ProviderOptions"));
services.Configure<StorageSettings>(config.GetSection("StorageSettings"));

services.AddScoped<ApiDataProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var apiUrl = config["ApiUrl"] ?? string.Empty;

    return new ApiDataProvider(httpClient, apiUrl);
});
services.AddScoped<CsvDataProvider>();
services.AddScoped<JsonDataProvider>();
services.AddSingleton<IProviderFactory, ProviderFactory>();

services.AddScoped<IDataProvider>(sp => sp.GetRequiredService<IProviderFactory>().Create());

ServiceProvider serviceProvider = services.BuildServiceProvider();

// Main Flow với Global Exception Handling
Log.Information(">>> Program Started (Press Ctrl+C to Exit) <<<");
int totalLine = 0;

while (true)
{
    using (var scope = serviceProvider.CreateScope())
    {
        var provider = scope.ServiceProvider;

        var providerOptions = provider.GetRequiredService<IOptionsMonitor<ProviderOptions>>();
        var storageSettings = provider.GetRequiredService<IOptionsMonitor<StorageSettings>>();
        var dataProvider = provider.GetRequiredService<IDataProvider>();

        var mode = providerOptions.CurrentValue.ProviderType;

        string dir = storageSettings.CurrentValue.OutputPath ?? "Reports";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string summaryPath = Path.Combine(dir, "summary.csv");

        var startTime = DateTime.Now;
        var sw = Stopwatch.StartNew();

        Log.Information(">>> Application Started: Mode [{Mode}] <<<", mode);
        Log.Information("[Starting: {StartTime}]", startTime);
        Console.WriteLine("--------------------------------------------------");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            if (!File.Exists(summaryPath))
            {
                await File.WriteAllTextAsync(summaryPath, "Timestamp,Source,Description,Metric,Value\n");
            }

            if (providerOptions.CurrentValue.ProviderType.Equals("CSV"))
            {
                var data = await dataProvider.GetDataAsync<HousePrice>(cts.Token);
                var totalLinesInCsv = data.Count();

                var summaryByFurnishing = data
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

                Log.Information("Read {Count} rows from CSV file.", totalLinesInCsv);
                totalLine += totalLinesInCsv;

                Console.WriteLine("\n--- HOUSE PRICE SUMMARY BY FURNISHING ---");
                Console.WriteLine($"{"Status",-20} | {"Count",-10} | {"Avg Price (USD)",-15} | {"Max Price",-15}");
                Console.WriteLine(new string('-', 65));

                foreach (var item in summaryByFurnishing)
                {
                    Console.WriteLine($"{item.Status,-20} | {item.Count,-10} | {item.AvgPrice:N0} | {item.MaxPrice,15:N0}");
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},CSV,Average house price by furnishing,AvgPrice_{item.Status},{item.AvgPrice:F2}\n";
                    await File.AppendAllTextAsync(summaryPath, line);
                }

                Log.Information("Summary exported to {Path}", summaryPath);
            }
            else if (providerOptions.CurrentValue.ProviderType.Equals("API"))
            {
                var data = await dataProvider.GetDataAsync<Product>(cts.Token);
                var totalObjectsInAPI = data.Count();

                List<Product> premiumProducts = data
                    .Where(p => p.Price > 500 && p.Rating >= 4.0)
                    .OrderByDescending(p => p.Price)
                    .ToList();

                Log.Information("Fetched {Count} products from API.", totalObjectsInAPI);
                totalLine += totalObjectsInAPI;

                Console.WriteLine("\n--- TOP PREMIUM PRODUCTS ---");
                foreach (var p in premiumProducts)
                {
                    Console.WriteLine($"[{p.Category}] {p.Title,-20} | Price: ${p.Price,-8} | Rating: {p.Rating}");
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},API,Top premium product,PremiumProduct_{p.Title},{p.Price}\n";
                    await File.AppendAllTextAsync(summaryPath, line);
                }
                Log.Information("API Summary exported.");
            }
            else if (providerOptions.CurrentValue.ProviderType.Equals("JSON"))
            {
                var data = await dataProvider.GetDataAsync<Product>(cts.Token);
                var totalObjectsInJson = data.Count();

                List<Product> bestSellersByCategory = data
                    .GroupBy(p => p.Category)
                    .Select(g => g.OrderByDescending(p => p.Rating)
                                .ThenByDescending(p => p.Stock)
                                .First())
                    .OrderByDescending(p => p.Rating)
                    .ToList();

                Log.Information("Fetched {Count} products from API.", totalObjectsInJson);
                totalLine += totalObjectsInJson;

                Console.WriteLine("\n--- BEST SELLER PRODUCT BY CATEGORY ---");
                Console.WriteLine($"\n{"CATEGORY",-15} | {"TOP PRODUCT",-35} | {"PRICE",-8} | {"RATING",-6} | {"STOCK",-5}");
                Console.WriteLine(new string('-', 80));

                foreach (var p in bestSellersByCategory)
                {
                    Console.WriteLine($"{p.Category,-15} | {p.Title,-35} | ${p.Price,-7} | {p.Rating,-6} | {p.Stock,-5}");
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},JSON,Best seller products by category,{p.Category}_{p.Title},{p.Price}\n";
                    await File.AppendAllTextAsync(summaryPath, line);
                }
                //* -- End: Console Query --


                Log.Information("JSON Summary exported.");
            }
            else
            {
                Log.Warning("Invalid provider type! Please select approriate provider.");
            }

            sw.Stop();
            var endTime = DateTime.Now;
            var executionTime = sw.Elapsed.TotalMilliseconds;
            Log.Information("[Ending: {EndTime}]", endTime);
            Log.Information("[Execution time: {ExecutionTime} ms]", executionTime);
            Log.Information("[Total executed line: {TotalLine}]", totalLine);
            Log.Information(">>> Processing Completed Successfully <<<");
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Operation is cancelled!");
        }
        catch (Exception ex)
        {
            // Global Exception Handling Requirement
            Log.Error(ex, "An unhandled exception occurred during execution.");
            throw;
        }
    }
    Console.WriteLine("\n--------------------------------------------------");
    Console.WriteLine("Change 'ProviderType' in appsettings.json and press any key to run again...");
    Console.ReadKey();
}