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
IFluentAction<TResult> Cache(GetCachedValue<TResult> getCachedValue, SetCachedValue<TResult> setCachedValue, TimeSpan ttl);
IFluentAction<TResult> Cache(string key, ICache cache, TimeSpan ttl);
IFluentAction<TResult> SetCacheTtl(TimeSpan ttl);
IFluentAction<TResult> Fresh();
IFluentAction<TResult> RefreshIfInCache(Action<TResult> freshResult, CancellationToken cancellationToken = default);
IFluentAction<TResult> NoCache();
IFluentAction<TResult> OnStart(Action startAction);
IFluentAction<TResult> OnComplete(Action<TResult> completeAction);
IFluentAction<TResult> OnError(Action<Exception> errorAction);
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
private void Awake()
{
    _achievementService = SampleReadyApp.I.GetService<IAchievementService>();

    closeButton.onClick.AddListener(OnCloseButtonClick);
    pullToRefresh.RefreshRequested += () => FetchItems(forceFresh: true).ExecuteAsync();

    itemTemplate.gameObject.SetActive(false);
}

private void OnEnable()
{
	loadingOverlay.SetActive(false);
	noDataFallback.SetActive(false);
	errorFallback.gameObject.SetActive(false);

	FetchItems().ExecuteAsync(); // or await FetchItems();
}

private IFluentAction<List<AchievementData>> FetchItems(bool forceFresh = false)
{
	var action = _achievementService
		.GetAchievementsForThisApp(20, cancellationToken: this.GetDisableCancellationToken())
		.Retry() // Specify retry policy using default settings
		.RefreshIfInCache(OnFetchSuccess, this.GetAppQuitCancellationToken()) // Refresh query in the background if a result there is in the cache
		.OnStart(OnFetchStart)
		.OnComplete(OnFetchSuccess)
		.OnError(OnFetchError); // Intercept an exception to handle this case 

	if (forceFresh)
	{
		action = action.Fresh();
	}
	
	return action;
}

private void OnFetchStart()
{
	loadingOverlay.SetActive(true);
	noDataFallback.SetActive(false);
	errorFallback.gameObject.SetActive(false);
}

private void OnFetchSuccess(List<AchievementData> achievements)
{
	loadingOverlay.SetActive(false);

	if (achievements.Count == 0)
	{
		noDataFallback.SetActive(true);
	}
	else
	{
		Populate(achievements);
	}
}

private void OnFetchError(System.Exception exception)
{
	loadingOverlay.SetActive(false);
	errorFallback.gameObject.SetActive(true);
	errorFallback.text = $"Oops! Something went wrong. Please try again.\n\n<size=15>Error: {exception.Message}";
	Clear();
}
```

In this example, the `FetchAppAchievements` method demonstrates how to:
- Call the `GetAchievementsForThisApp` method with caching and retry policies.
- Automatically retry the operation if it fails, with default settings.
- Refresh the data if the result is retrieved from the cache.