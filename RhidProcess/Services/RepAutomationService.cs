using RhidProcess.Abstractions;
using RhidProcess.Browser;
using RhidProcess.Models;

namespace RhidProcess.Services;

public sealed class RepAutomationService(IBrowserFactory browserFactory)
{
    public async Task<UnlockResponse> ExecuteAsync(UnlockRequest request, CancellationToken cancellationToken = default)
    {
        await using var session =
            await BrowserSession.CreateAsync(browserFactory);

        await session.Login.LoginAsync();

        await session.Unlock.OpenAsync();

        await session.Unlock.FillAsync(
            request.Serial,
            request.Password);

        var contraSenha =
            await session.Unlock.GetContraSenhaAsync();

        return new UnlockResponse(contraSenha);
    }
}
