namespace RhidProcess.Options;

public sealed class RhidOptions
{
    public const string SectionName = "Rhid";

    public string BaseUrl { get; init; } = "https://www.rhid.com.br";
    public string LoginRoute { get; init; } = "/v2/#/login";
    public string UnlockRoute { get; init; } = "/v2/#/desbloqueio_rep_violacao";
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public int NavigationTimeoutSeconds { get; init; } = 30;
    public int ActionTimeoutSeconds { get; init; } = 30;

    public bool HasRequiredCredentials =>
        !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Password);
}
