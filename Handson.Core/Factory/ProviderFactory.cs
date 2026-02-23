using Handson.Core.Providers;
using Handson.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Handson.Core;

public class ProviderFactory : IProviderFactory
{
    private readonly IServiceProvider _sp;
    private readonly IOptionsMonitor<ProviderOptions> _providerOptions;

    public ProviderFactory(IServiceProvider sp, IOptionsMonitor<ProviderOptions> providerOptions)
    {
        _sp = sp;
        _providerOptions = providerOptions;
    }

    public IDataProvider Create()
    {
        if (_providerOptions.CurrentValue.ProviderType.Equals("CSV"))
        {
            return _sp.GetRequiredService<CsvDataProvider>();
        }
        else if (_providerOptions.CurrentValue.ProviderType.Equals("JSON"))
        {
            return _sp.GetRequiredService<JsonDataProvider>();
        }
        else
            return _sp.GetRequiredService<ApiDataProvider>();
    }
}
