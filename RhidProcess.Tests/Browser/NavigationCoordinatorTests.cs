using RhidProcess.Browser;

namespace RhidProcess.Tests.Browser;

public sealed class NavigationCoordinatorTests
{
    [Fact]
    public async Task WaitForNavigationAndActionAsync_RegistersNavigationBeforeAction()
    {
        var calls = new List<string>();
        var navigationCompletion =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task BeginNavigation()
        {
            calls.Add("navigation");
            return navigationCompletion.Task;
        }

        Task Click()
        {
            calls.Add("click");
            navigationCompletion.SetResult();
            return Task.CompletedTask;
        }

        await NavigationCoordinator.WaitForNavigationAndActionAsync(
            BeginNavigation,
            Click,
            CancellationToken.None);

        Assert.Equal(["navigation", "click"], calls);
    }
}
