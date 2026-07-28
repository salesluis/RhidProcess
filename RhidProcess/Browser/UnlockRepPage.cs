using PuppeteerSharp;

namespace RhidProcess.Browser;

public class UnlockRepPage(
    IPage page,
    RhidOptions options)
{
    
    private const string Serial = "input[placeholder='Serial']";
    private const string Password = "input[placeholder='Senha']";
    private const string Button = "#btnSave";
    private const string Result = ".form-control.ng-binding.ng-scope";
    
    public async Task OpenAsync()
    {
        var url = new Uri($"{options.BaseUrl}{options.UnlockRoute}").ToString();
        await page.GoToAsync(
            url,
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

        var contraSenha =  await page.EvaluateExpressionAsync<string>($"document.querySelector('{Result}').innerText");
        return contraSenha;
    }
}