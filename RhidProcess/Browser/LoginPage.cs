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
        
        Configuration.Deconstruct(out var email, out var password);
        
        await page
            .Locator(EmailSelector)
            .FillAsync(email);

        await page
            .Locator(PasswordSelector)
            .FillAsync(password);

        await page
            .Locator(SubmitSelector)
            .ClickAsync();

        await page.WaitForNavigationAsync();
    }
}