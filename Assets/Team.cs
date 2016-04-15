using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Team : MonoBehaviour {
	public Color teamColor;

	public List<GameObject> members;

	// Use this for initialization
	void Start () {
		foreach(Transform child in transform){
			if (child.tag == "Player"){
				members.Add(child.gameObject);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public List<GameObject> GetMembers(){
		return members;
	}

	public void EnableCameras(){
		foreach(GameObject member in members){
			member.transform.FindChild("Camera").GetComponent<Camera>().enabled = true;
		}
	}

	public void DisableCameras(){
		foreach(GameObject member in members){
			member.transform.FindChild("Camera").GetComponent<Camera>().enabled = false;
		}
	}
}
