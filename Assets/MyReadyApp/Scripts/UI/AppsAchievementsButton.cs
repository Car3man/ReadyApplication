using UnityEngine;
using UnityEngine.UI;

public class AppsAchievementsButton : MonoBehaviour
{
	[SerializeField] private Button button;
	[SerializeField] private AppsAchievementsPanel panel;

	private void Start()
	{
		button.onClick.AddListener(OnButtonClick);
	}

	private void OnButtonClick()
	{
		panel.gameObject.SetActive(true);
	}
}
