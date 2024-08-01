using UnityEngine;

public class Spinner : MonoBehaviour
{
	[SerializeField] private float speed = 360f;

	private void Update()
	{
		transform.Rotate(Vector3.forward, -speed * Time.deltaTime);
	}
}
