using PuppeteerSharp;

namespace RhidProcess.Browser;

public class UnlockRepPage(IPage page)
{
    public const string Serial = "input[placeholder='Serial']";
    public const string Password = "input[placeholder='Senha']";
    public const string Button = "#btnSave";
    public const string Result = ".form-control.ng-binding.ng-scope";
    public const string UnlockRoute = "/v2/#/desbloqueio_rep_violacao";

    
    public async Task OpenAsync()
    {
        await page.GoToAsync(
            $"{Configuration.BaseUrl}{UnlockRoute}",
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
            await page.Locator(Serial)
                .FillAsync(serial);

            await page.Locator(Password)
                .FillAsync(password);

            await page.Locator(Button)
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
            Result);

        return page.Locator(
            Result).ToString() ?? "";
    }
}