using PuppeteerSharp;

namespace RhidProcess.Browser;

public class LoginPage(IPage page)
{
    private const string Email = "#email";
    private const string Password = "#password";
    private const string Submit = "#m_login_signin_submit";
    public async Task LoginAsync()
    {
        await page.GoToAsync(
            $"{Configuration.BaseUrl}{Configuration.LoginRoute}",
            new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2]
            });

        await page.Locator(Email)
            .FillAsync(Configuration.Email);

        await page.Locator(Password)
            .FillAsync(Configuration.Password);

        await page.Locator(Submit)
            .ClickAsync();

        await page.WaitForNavigationAsync();
    }
}