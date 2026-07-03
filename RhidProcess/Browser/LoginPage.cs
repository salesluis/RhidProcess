using PuppeteerSharp;

namespace RhidProcess.Browser;

public class LoginPage(IPage page)
{
    private const string EmailSelector = "#email";
    private const string PasswordSelector = "#password";
    private const string SubmitSelector = "#m_login_signin_submit";
    public async Task LoginAsync()
    {
        await page.GoToAsync(
            $"{Configuration.BaseUrl}{Configuration.LoginRoute}",
            new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2]
            });

        await page.Locator(EmailSelector)
            .FillAsync(Configuration.Email);

        await page.Locator(PasswordSelector)
            .FillAsync(Configuration.Password);

        await page.Locator(SubmitSelector)
            .ClickAsync();

        await page.WaitForNavigationAsync();
    }
}