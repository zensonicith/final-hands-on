using System;
using System.Collections.Generic;
using System.Text;

namespace Handson.Core.Providers
{
    public interface IDataProvider
    {
        Task<IEnumerable<T>> GetDataAsync<T>();
    }
}
