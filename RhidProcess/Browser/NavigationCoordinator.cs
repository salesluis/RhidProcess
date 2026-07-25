namespace RhidProcess.Browser;

internal static class NavigationCoordinator
{
    public static async Task WaitForNavigationAndActionAsync(
        Func<Task> beginNavigation,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        var navigationTask = beginNavigation();
        var actionTask = action();

        await Task
            .WhenAll(navigationTask, actionTask)
            .WaitAsync(cancellationToken);
    }
}
