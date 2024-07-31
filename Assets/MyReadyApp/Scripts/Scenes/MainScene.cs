using System.Collections.Generic;
using ReadyApplication.Core;
using ReadyApplication.Standard;
using RGN.Modules.Achievement;
using UnityEngine;

public class MainScene : MonoBehaviour
{
	private IAchievementService _achievementService;

	private void Awake()
	{
		_achievementService = SampleReadyApp.I.GetService<IAchievementService>();
		_achievementService.CachedAchievementUpdated += OnCachedAchievementUpdate;
		_achievementService.CachedAchievementsUpdated += OnCachedAchievementsUpdate;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			FetchAppAchievements();
		}
	}

	private async void FetchAppAchievements()
	{
		List<AchievementData> achievements = await _achievementService
			.GetAchievementsForThisApp(5, cancellationToken: this.GetDestroyCancellationToken())
			.Retry() // Specify retry policy (by default 3 attempts with 1000 backoff)
			.RefreshIfInCache(OnAchievementsFetch, this.GetAppQuitCancellationToken()); // Request fresh data if result is from cache
		OnAchievementsFetch(achievements);
	}

	private void OnAchievementsFetch(List<AchievementData> achievements)
	{
		Debug.Log("Fetched achievements: " + achievements.Count);
	}

	private void OnCachedAchievementUpdate(AchievementData achievement)
	{
		Debug.Log("Cached achievement updated: " + achievement);
	}

	private void OnCachedAchievementsUpdate()
	{
		Debug.Log("Cached achievements updated");
	}
}
