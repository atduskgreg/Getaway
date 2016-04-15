using UnityEngine;
using System.Collections;

public class FollowPlayer : MonoBehaviour {
	public GameObject player;
	public float followSpeed = 0.1f;

	private Vector3 startingPosition;

	// Use this for initialization
	void Start () {
		startingPosition = new Vector3 (transform.position.x, transform.position.y, transform.position.z);
	
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 newPosition = Vector3.Lerp(transform.position, player.transform.position, followSpeed);
		newPosition.y = startingPosition.y;

		transform.position = newPosition;
	}
}
