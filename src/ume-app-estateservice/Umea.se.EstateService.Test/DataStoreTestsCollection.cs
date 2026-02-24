namespace Umea.se.EstateService.Test;

/// <summary>
/// Collection for tests that mutate the shared IDataStore. Parallelization is disabled to avoid contention.
/// </summary>
[CollectionDefinition("DataStoreTests", DisableParallelization = true)]
public class DataStoreTestsCollection
{
}

