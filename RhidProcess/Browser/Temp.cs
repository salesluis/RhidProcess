using RhidProcess.Abstractions;
using RhidProcess.Models;

namespace RhidProcess.Browser;

#region Selectors

public static class LoginSelectors
{
    public const string Email = "#email";
    public const string Password = "#password";
    public const string Submit = "#m_login_signin_submit";
}

public static class UnlockSelectors
{
    public const string Serial = "input[placeholder='Serial']";
    public const string Password = "input[placeholder='Senha']";
    public const string Button = "#btnSave";
    public const string Result = ".form-control.ng-binding.ng-scope";
}

#endregion

#region Routes

public static class Routes
{
    public const string Login = "/v2/#/login";
    public const string Unlock = "/v2/#/desbloqueio_rep_violacao";
}

#endregion

#region Environment

public static class Env
{
    public static string BaseUrl => "https://site.com";

    public static string Email => "usuario@email.com";

    public static string Password => "senha";
}

#endregion
