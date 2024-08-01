using ReadyApplication.Core;
using ReadyApplication.Standard;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScene : MonoBehaviour
{
	public async void Start()
	{
		Application.targetFrameRate = 60;
		// Full initialization of the ReadyApp SDK
		// We await the initialization, the first authentication process and also post initialization (can be defined by user)
		await SampleReadyApp.I.InitializeAsync(gameObject.GetDestroyCancellationToken());
		SceneManager.LoadScene("MainScene");
	}
}
