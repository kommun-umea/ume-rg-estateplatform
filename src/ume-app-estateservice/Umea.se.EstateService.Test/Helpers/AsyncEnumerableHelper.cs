namespace Umea.se.EstateService.Test.Helpers;

public static class AsyncEnumerableHelper
{
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;

        yield break;
    }
}
