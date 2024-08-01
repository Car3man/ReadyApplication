using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PullToRefresh : MonoBehaviour, IEndDragHandler
{
	public System.Action RefreshRequested;

	[SerializeField] private RectTransform target;
	[SerializeField] private float offset;
	[SerializeField] private float distance;
	[SerializeField] private AnimationCurve curve;

	private ScrollRect _scrollRect;

	private void Start()
	{
		_scrollRect = GetComponent<ScrollRect>();
	}

	private void Update()
	{
		Vector2 contentAnchoredPosition = _scrollRect.content.anchoredPosition;
		float position = Mathf.Clamp(-contentAnchoredPosition.y + offset, 0f, distance);
		target.anchoredPosition = new Vector2(0, (target.sizeDelta.y / 2f) + -curve.Evaluate(position / distance) * distance);
		target.rotation = Quaternion.Euler(0, 0, -curve.Evaluate(position / distance) * 360f);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		Vector2 contentAnchoredPosition = _scrollRect.content.anchoredPosition;
		if (contentAnchoredPosition.y > -distance + offset)
		{
			return;
		}

		RefreshRequested?.Invoke();
	}
}
