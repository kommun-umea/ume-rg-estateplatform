using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.HostedServices;

public sealed class DataSyncServiceTests
{
    [Fact]
    public async Task TriggerManualRefreshAsync_WhenStartupRefreshIsRunning_ReturnsAlreadyRunning()
    {
        BlockingDataRefreshService dataRefreshService = new();
        InMemoryDataStore dataStore = new();
        NullDataStorePersistence persistence = new();

        ApplicationConfig appConfig = BuildAppConfig(new Dictionary<string, string?>
        {
            ["DataSync:RefreshIntervalHours"] = "1",
            ["DataSync:MaxRetries"] = "0",
            ["DataSync:RetryBaseDelaySeconds"] = "1"
        });

        SearchHandler searchHandler = CreateSearchHandler(appConfig);

        DataSyncService sut = new(
            dataRefreshService,
            dataStore,
            persistence,
            new BuildingBackgroundCache(new FakePythagorasClient(), dataStore, NullLogger<BuildingBackgroundCache>.Instance),
            searchHandler,
            appConfig,
            NullLogger<DataSyncService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        try
        {
            await dataRefreshService.WaitUntilRefreshStartedAsync(TimeSpan.FromSeconds(2));

            RefreshStatus status = await sut.TriggerManualRefreshAsync();

            status.ShouldBe(RefreshStatus.AlreadyRunning);
        }
        finally
        {
            dataRefreshService.Release();
            await sut.StopAsync(CancellationToken.None);
        }
    }

    private static ApplicationConfig BuildAppConfig(Dictionary<string, string?> overrides)
    {
        string testProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(testProjectRoot, "appsettings.unittests.json"))
            .AddInMemoryCollection(overrides)
            .Build();
        return new ApplicationConfig(configuration, typeof(Program).Assembly);
    }

    private static SearchHandler CreateSearchHandler(ApplicationConfig appConfig)
    {
        return new SearchHandler(new EmptyDocumentProvider(), appConfig);
    }

    private sealed class BlockingDataRefreshService : IDataRefreshService
    {
        private readonly TaskCompletionSource _startedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task RefreshDataAsync(CancellationToken cancellationToken = default)
        {
            _startedTcs.TrySetResult();
            await _releaseTcs.Task.WaitAsync(cancellationToken);
        }

        public Task WaitUntilRefreshStartedAsync(TimeSpan timeout)
            => _startedTcs.Task.WaitAsync(timeout);

        public void Release()
            => _releaseTcs.TrySetResult();
    }

    private sealed class EmptyDocumentProvider : IPythagorasDocumentProvider
    {
        public Task<ICollection<PythagorasDocument>> GetDocumentsAsync()
            => Task.FromResult<ICollection<PythagorasDocument>>([]);
    }
}
