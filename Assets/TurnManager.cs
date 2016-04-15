using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour {
	public GameObject[] teams;
	int currentTeam = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.E)){
			EndTurn();
		}
	}

	void EndTurn(){
		CurrentTeam().GetComponent<Team>().DisableCameras();
		OtherTeam().GetComponent<Team>().EnableCameras();
		NextTeam();
	}

	void NextTeam(){
		currentTeam++;
		if(currentTeam >= teams.Length){
			currentTeam = 0;
		}
	}

	public Team CurrentTeam(){
		return teams[currentTeam].GetComponent<Team>();
	}

	public Team OtherTeam(){
		if(currentTeam == 0){
			return teams[1].GetComponent<Team>();
		} else {
			return teams[0].GetComponent<Team>();
		}
	}
}
