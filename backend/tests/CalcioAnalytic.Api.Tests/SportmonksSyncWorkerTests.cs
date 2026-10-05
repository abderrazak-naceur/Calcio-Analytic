using CalcioAnalytic.Workers;

namespace CalcioAnalytic.Api.Tests;

public sealed class SportmonksSyncWorkerTests
{
    [Fact]
    public void Sync_is_disabled_without_explicit_opt_in()
    {
        var options = new WorkerOptions();

        Assert.False(SportmonksSyncWorker.IsEnabled(options, "token"));
    }

    [Fact]
    public void Sync_is_enabled_only_when_opted_in_and_token_exists()
    {
        var options = new WorkerOptions { SportmonksSyncEnabled = true };

        Assert.True(SportmonksSyncWorker.IsEnabled(options, "token"));
        Assert.False(SportmonksSyncWorker.IsEnabled(options, null));
        Assert.False(SportmonksSyncWorker.IsEnabled(options, " "));
    }
}
