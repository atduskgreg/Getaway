using UnityEngine;
using System.Collections;

public class KeyboardControl : MonoBehaviour {
	public float moveSpeed = 1.0f;
	public GameObject calibrationCube;

	private float d;

	public float standingHeight = 2.4f;
	public float duckingHeight = 0.75f;

	private bool isDucking = false;

	// Use this for initialization
	void Start () {
//		d = calibrationCube.transform.lossyScale.x;
		d= 30.0f;
		print ("cube scale: " + d);
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 p = transform.position;

		if (Input.GetKeyDown ("w")) {
			p.z += d;
		}
		if (Input.GetKeyDown ("a")) {
			p.x -= d;
		}
		if (Input.GetKeyDown ("s")) {
			p.z -= d;
		}
		if (Input.GetKeyDown ("d")) {
			p.x += d;
		}

		if (Input.GetKeyDown ("space")) {
			isDucking = !isDucking;
		}

		if (isDucking) {
			p.y = duckingHeight;
		} else {
			p.y = standingHeight;
		}

		transform.position = p;
	}
}
