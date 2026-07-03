using PuppeteerSharp;

namespace RhidProcess.Browser;

public class LoginPage(IPage page)
{
    public const string Email = "#email";
    public const string Password = "#password";
    public const string Submit = "#m_login_signin_submit";
    public const string LoginRoute = "/v2/#/login";
    
    public async Task LoginAsync()
    {
        await page.GoToAsync(
            $"{Configuration.BaseUrl}{LoginRoute}",
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