## Key Features

### 1. ReadyManager: Simplified Initialization
The `BaseReadyApp` is go-to component for initializing the SDK. It ensures the SDK is fully initialized, including basic setup and first authentication, before proceeding with any operations. Also it waits for custom post methods.

### 2. Flexible Cache Layer
Each module in the toolkit supports a cache layer, allowing you to configure cache policies flexibly for each calling method.

### 3. FluentAction Interface
Almost all methods in the toolkit implement the `FluentAction` interface, enabling you to configure each call with a variety of policies such as retry, fallback, and cache. The available APIs within the `FluentAction` interface are:

```csharp
IFluentAction<TResult> Retry(int maxAttempts = 3, int minBackoff = 1000);
IFluentAction<TResult> RetryWhen<TException>() where TException : Exception;
IFluentAction<TResult> RetryWhen<TException>(Func<TException, bool> selector) where TException : Exception;
IFluentAction<TResult> NoRetry();
IFluentAction<TResult> Fallback(Func<Task<TResult>> fallbackFunc);
IFluentAction<TResult> FallbackWhen<TException>() where TException : Exception;
IFluentAction<TResult> FallbackWhen<TException>(Func<TException, bool> selector) where TException : Exception;
IFluentAction<TResult> NoFallback();
IFluentAction<TResult> OnComplete(Action<TResult> completeAction);
IFluentAction<TResult> Cache(GetCachedValue<TResult> getCachedValue, SetCachedValue<TResult> setCachedValue, TimeSpan ttl);
IFluentAction<TResult> Cache(string key, ICache cache, TimeSpan ttl);
IFluentAction<TResult> SetCacheTtl(TimeSpan ttl);
IFluentAction<TResult> Fresh();
IFluentAction<TResult> RefreshIfInCache(Action<TResult> freshResult, CancellationToken cancellationToken = default);
IFluentAction<TResult> NoCache();
```

- **Retry Policy**: Configure retry attempts and backoff strategies for resilient operations.
- **Fallback Policy**: Define fallback actions to maintain functionality during failures.
- **Cache Policy**: Implement flexible caching strategies to optimize data retrieval and performance.

### 4. Standard Implementation
The toolkit includes a "Standard" implementation, featuring completed repositories, services, and other components. This implementation allows developers to utilize call-only services, providing access to cached entities and subscription to updates. For example, the `IAchievementService` interface:

```csharp
public interface IAchievementService
{
    RepositoryEntityCache<string, AchievementData> CachedAchievements { get; }
    RepositoryEntityCache<string, UserAchievement> CachedUserAchievements { get; }
    event System.Action<AchievementData> CachedAchievementUpdated;
    event System.Action<UserAchievement> CachedUserAchievementUpdated;
    event System.Action CachedAchievementsUpdated;
    event System.Action CachedUserAchievementsUpdated;
    // Additional methods and events
}
```

### 5. Automatic Cache Invalidation
The toolkit ensures that caches related to specific operations are automatically invalidated when necessary. For instance, when calling `StoreService.BuyVirtualItem`, the caches related to inventory and currency are invalidated to maintain data consistency and accuracy. (Need to be improved)

## Example Usage

```csharp
private async void FetchAppAchievements()
{
    List<AchievementData> achievements = await _achievementService
        .GetAchievementsForThisApp(5, cancellationToken: this.GetDestroyCancellationToken())
        .Retry() // Specify retry policy (by default 3 attempts with 1000ms backoff)
        .RefreshIfInCache(OnAchievementsFetch, this.GetAppQuitCancellationToken()); // Request fresh data if result is from cache
    OnAchievementsFetch(achievements);
}

private void OnAchievementsFetch(List<AchievementData> achievements)
{
    Debug.Log("Fetched achievements: " + achievements.Count);
}
```

In this example, the `FetchAppAchievements` method demonstrates how to:
- Call the `GetAchievementsForThisApp` method with caching and retry policies.
- Automatically retry the operation if it fails, with default settings.
- Refresh the data if the result is retrieved from the cache.