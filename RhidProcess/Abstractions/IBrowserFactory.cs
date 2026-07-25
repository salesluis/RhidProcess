using PuppeteerSharp;

namespace RhidProcess.Abstractions;

public interface IBrowserFactory
{
    Task<IBrowser> CreateBrowserAsync(CancellationToken cancellationToken = default);
}
