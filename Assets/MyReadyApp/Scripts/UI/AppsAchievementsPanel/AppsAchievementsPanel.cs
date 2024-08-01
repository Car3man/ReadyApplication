using System.Collections.Generic;
using ReadyApplication.Core;
using ReadyApplication.Standard;
using RGN.Modules.Achievement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AppsAchievementsPanel : MonoBehaviour
{
	[SerializeField] private Button closeButton;
	[SerializeField] private PullToRefresh pullToRefresh;
	[SerializeField] private AppsAchievementsPanelItem itemTemplate;
	[SerializeField] private RectTransform content;
	[SerializeField] private GameObject loadingOverlay;
	[SerializeField] private GameObject noDataFallback;
	[SerializeField] private TextMeshProUGUI errorFallback;

	private IAchievementService _achievementService;
	private readonly List<AppsAchievementsPanelItem> _items = new();

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

		FetchItems().ExecuteAsync();
		// or
		// await FetchItems();
	}

	private IFluentAction<List<AchievementData>> FetchItems(bool forceFresh = false)
	{
		var action = _achievementService
			.GetAchievementsForThisApp(20, cancellationToken: this.GetDisableCancellationToken())
			.Retry()
			.RefreshIfInCache(OnFetchSuccess, this.GetAppQuitCancellationToken())
			.OnStart(OnFetchStart)
			.OnComplete(OnFetchSuccess)
			.OnError(OnFetchError);

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

	private void Populate(List<AchievementData> achievements)
	{
		Clear();

		foreach (var achievement in achievements)
		{
			AppsAchievementsPanelItem item = Instantiate(itemTemplate, content);
			item.gameObject.SetActive(true);
			item.IdText.text = achievement.id;
			item.NameText.text = achievement.name;
			item.DescriptionText.text = achievement.description;
			_items.Add(item);
		}
	}

	private void Clear()
	{
		foreach (var item in _items)
		{
			Destroy(item.gameObject);
		}
		_items.Clear();
	}

	private void OnCloseButtonClick()
	{
		gameObject.SetActive(false);
	}
}
