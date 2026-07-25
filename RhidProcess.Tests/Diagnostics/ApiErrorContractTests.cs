using System.Text.Json;
using RhidProcess.Diagnostics;
using RhidProcess.Models;

namespace RhidProcess.Tests.Diagnostics;

public sealed class ApiErrorContractTests
{
    [Fact]
    public void ApiErrorResponse_UsesExpectedPublicJsonContract()
    {
        var response = new ApiErrorResponse(
            "error-123",
            AutomationErrorCodes.LoginNotConfirmed,
            AutomationStages.LoginSubmit,
            "Não foi possível confirmar o login no RHID.");

        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("error-123", root.GetProperty("errorId").GetString());
        Assert.Equal("RHID_LOGIN_NOT_CONFIRMED", root.GetProperty("code").GetString());
        Assert.Equal("login_submit", root.GetProperty("stage").GetString());
        Assert.Equal(
            "Não foi possível confirmar o login no RHID.",
            root.GetProperty("message").GetString());
        Assert.Equal(4, root.EnumerateObject().Count());
        Assert.False(root.TryGetProperty("stackTrace", out _));
        Assert.False(root.TryGetProperty("stackTace", out _));
    }

    [Fact]
    public void RhidAutomationException_PreservesPublicClassification()
    {
        var cause = new TimeoutException("upstream detail");
        var exception = new RhidAutomationException(
            AutomationErrorCodes.UpstreamTimeout,
            AutomationStages.LoginSubmit,
            "O portal RHID não respondeu no tempo esperado.",
            504,
            cause);

        Assert.Equal("RHID_UPSTREAM_TIMEOUT", exception.Code);
        Assert.Equal("login_submit", exception.Stage);
        Assert.Equal("O portal RHID não respondeu no tempo esperado.", exception.PublicMessage);
        Assert.Equal(504, exception.StatusCode);
        Assert.Same(cause, exception.InnerException);
    }

    [Fact]
    public void ErrorCodesAndStages_AreStableAndUnique()
    {
        var errorCodes = new[]
        {
            AutomationErrorCodes.ConfigurationInvalid,
            AutomationErrorCodes.UpstreamTimeout,
            AutomationErrorCodes.LoginNotConfirmed,
            AutomationErrorCodes.UpstreamFailure,
            AutomationErrorCodes.InternalError
        };
        var stages = new[]
        {
            AutomationStages.Request,
            AutomationStages.Configuration,
            AutomationStages.BrowserStartup,
            AutomationStages.LoginPage,
            AutomationStages.LoginSubmit,
            AutomationStages.UnlockPage,
            AutomationStages.UnlockSubmit,
            AutomationStages.ResultRead
        };

        Assert.Equal(errorCodes.Length, errorCodes.Distinct().Count());
        Assert.Equal(stages.Length, stages.Distinct().Count());
        Assert.All(errorCodes, value => Assert.Equal(value.ToUpperInvariant(), value));
        Assert.All(stages, value => Assert.Equal(value.ToLowerInvariant(), value));
    }
}
