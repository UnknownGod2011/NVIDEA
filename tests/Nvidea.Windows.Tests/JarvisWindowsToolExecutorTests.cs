using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Nvidea.Core.Desktop;
using Nvidea.Windows;

namespace Nvidea.Windows.Tests;

public sealed class JarvisWindowsToolExecutorTests
{
    [Fact]
    public async Task Approval_prompt_is_marshaled_to_the_owner_dispatcher()
    {
        var result = new TaskCompletionSource<(bool ApprovalRanOnOwnerThread, JarvisToolExecution Execution)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var started = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                var owner = new Window();
                var dispatcher = owner.Dispatcher;
                var ownerThreadId = Environment.CurrentManagedThreadId;
                var approvalRanOnOwnerThread = false;
                var executor = new JarvisWindowsToolExecutor(
                    owner,
                    capturedTarget: null,
                    approval: _ =>
                    {
                        approvalRanOnOwnerThread = Environment.CurrentManagedThreadId == ownerThreadId;
                        return false;
                    });
                started.Set();

                _ = Task.Run(async () =>
                {
                    using var arguments = JsonDocument.Parse("{\"application\":\"notepad\"}");
                    return await executor.ExecuteAsync("open_application", arguments.RootElement);
                }).ContinueWith(task => dispatcher.BeginInvoke(() =>
                {
                    if (task.IsCompletedSuccessfully)
                        result.TrySetResult((approvalRanOnOwnerThread, task.Result));
                    else
                        result.TrySetException(task.Exception?.GetBaseException() ?? new InvalidOperationException("Tool execution failed."));
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }), TaskScheduler.Default);

                Dispatcher.Run();
            }
            catch (Exception exception)
            {
                result.TrySetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "keyboard-wtf-jarvis-dispatcher-test"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        var execution = await result.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
        Assert.True(execution.ApprovalRanOnOwnerThread);
        Assert.False(execution.Execution.Succeeded);
        Assert.Contains("not approved", execution.Execution.Message, StringComparison.OrdinalIgnoreCase);
    }
}
