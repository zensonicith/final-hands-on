using Handson.Core.Providers;

namespace Handson.Core;

public interface IProviderFactory
{
    IDataProvider Create();
}
