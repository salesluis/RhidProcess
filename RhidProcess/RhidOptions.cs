using System.ComponentModel.DataAnnotations;

namespace RhidProcess;


public sealed class RhidOptions
{
    public const string SectionName = "Rhid";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://www.rhid.com.br";

    [Required]
    public string LoginRoute { get; init; } = "/v2/#/login";

    [Required]
    public string UnlockRoute { get; init; } = "/v2/#/desbloqueio_rep_violacao";

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DefaultTimeout { get; init; } = 30_000;

    [Range(1, int.MaxValue)]
    public int DefaultNavigationTimeout { get; init; } = 30_000;
}
