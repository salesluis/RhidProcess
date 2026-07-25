using RhidProcess.Options;

namespace RhidProcess.Tests.Options;

public sealed class RhidOptionsTests
{
    [Theory]
    [InlineData("", "password")]
    [InlineData(" ", "password")]
    [InlineData("email@example.invalid", "")]
    [InlineData("email@example.invalid", " ")]
    public void HasRequiredCredentials_ReturnsFalseWhenAValueIsBlank(
        string email,
        string password)
    {
        var options = new RhidOptions
        {
            Email = email,
            Password = password
        };

        Assert.False(options.HasRequiredCredentials);
    }

    [Fact]
    public void HasRequiredCredentials_ReturnsTrueWhenBothValuesArePresent()
    {
        var options = new RhidOptions
        {
            Email = "service-account@example.invalid",
            Password = "configured-at-runtime"
        };

        Assert.True(options.HasRequiredCredentials);
    }

    [Fact]
    public void Defaults_UseSafeTimeoutsAndNoCompiledCredentials()
    {
        var options = new RhidOptions();

        Assert.Equal(30, options.NavigationTimeoutSeconds);
        Assert.Equal(30, options.ActionTimeoutSeconds);
        Assert.False(options.HasRequiredCredentials);
        Assert.Equal(string.Empty, options.Email);
        Assert.Equal(string.Empty, options.Password);
    }
}
