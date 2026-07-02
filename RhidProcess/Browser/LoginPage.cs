using PuppeteerSharp;

namespace RhidProcess.Browser;

public class LoginPage(IPage page)
{
    public async Task LoginAsync()
    {
        await page.GoToAsync(
            $"{Env.BaseUrl}{Routes.Login}",
            new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2]
            });

        await page.Locator(LoginSelectors.Email)
            .FillAsync(Env.Email);

        await page.Locator(LoginSelectors.Password)
            .FillAsync(Env.Password);

        await page.Locator(LoginSelectors.Submit)
            .ClickAsync();

        await page.WaitForNavigationAsync();
    }
}