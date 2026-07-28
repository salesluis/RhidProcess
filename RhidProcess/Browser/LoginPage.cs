using PuppeteerSharp;

namespace RhidProcess.Browser;

public class LoginPage(IPage page, RhidOptions options)
{
    private const string EmailSelector = "#email";
    private const string PasswordSelector = "#password";
    private const string SubmitSelector = "#m_login_signin_submit";
    public async Task LoginAsync()
    {
        var url = new Uri($"{options.BaseUrl}{options.LoginRoute}").ToString();

        await page.GoToAsync(
            url,
            new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2]
            });

        var email = options.Email;
        var password = options.Password;
        
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
