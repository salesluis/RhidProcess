namespace RhidProcess;


public static class Configuration
{
    
    public const string LoginRoute = "/v2/#/login";
    public const string UnlockRoute = "/v2/#/desbloqueio_rep_violacao";
    public static string BaseUrl => "https://www.rhid.com.br";
    public static string Email => "sac@salainformatica.com.br";
    public static string Password => "Suporte@2023";
    public static void Deconstruct(out string email, out string password)
    {
        email = Email;
        password = Password;
    }
}

