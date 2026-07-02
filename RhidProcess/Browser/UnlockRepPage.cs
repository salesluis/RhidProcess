using PuppeteerSharp;

namespace RhidProcess.Browser;

public class UnlockRepPage(IPage page)
{
    public async Task OpenAsync()
    {
        await page.GoToAsync(
            $"{Env.BaseUrl}{Routes.Unlock}",
            new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2]
            });

        await page.WaitForSelectorAsync("[id^=\"n_\"]");
    }

    public async Task FillAsync(string serial, string password)
    {
        try
        {
            await page.Locator(UnlockSelectors.Serial)
                .FillAsync(serial);

            await page.Locator(UnlockSelectors.Password)
                .FillAsync(password);

            await page.Locator(UnlockSelectors.Button)
                .ClickAsync();
        }
        catch
        {
            // fallback para páginas AngularJS antigas
            await page.EvaluateFunctionAsync(
                @"(serialValue, senhaValue) => {

                    const inputSerial =
                        document.querySelector('input[placeholder=""Serial""]');

                    const inputSenha =
                        document.querySelector('input[placeholder=""Senha""]');

                    const btn =
                        document.getElementById('btnSave');

                    if (!inputSerial || !inputSenha || !btn)
                        throw new Error('Campos não encontrados.');

                    inputSerial.value = serialValue;
                    inputSerial.dispatchEvent(
                        new Event('input', { bubbles: true })
                    );

                    inputSenha.value = senhaValue;
                    inputSenha.dispatchEvent(
                        new Event('input', { bubbles: true })
                    );

                    btn.click();

                }",
                serial,
                password);
        }
    }

    public async Task<string> GetContraSenhaAsync()
    {
        await page.WaitForSelectorAsync(
            UnlockSelectors.Result);

        return page.Locator(
            UnlockSelectors.Result).ToString() ?? "";
    }
}