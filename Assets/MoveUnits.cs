using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.ThirdPerson;

public class MoveUnits : MonoBehaviour {
	public LayerMask groundMask = -1;
	TurnManager turnManager;

	// Use this for initialization
	void Start () {
		turnManager = FindObjectOfType<TurnManager>();	
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKey(KeyCode.A)){
			Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

			foreach(GameObject character in turnManager.CurrentTeam().GetMembers()){
				Camera characterCamera = character.transform.Find("Camera").gameObject.GetComponent<Camera>();
				if(characterCamera.pixelRect.Contains(mousePos)){
					character.GetComponent<LookAtMouse>().DoLook();
					break;
				}
			}
		}
		
	}
	
	void OnGUI(){
		Event currentEvent = Event.current;
		
		if(currentEvent.type == EventType.MouseDown){

			// normalize mouse position
			Vector2 mousePos = new Vector2(currentEvent.mousePosition.x, -currentEvent.mousePosition.y + Screen.height );

			foreach(GameObject character in turnManager.CurrentTeam().GetMembers()){
				Camera characterCamera = character.transform.Find("Camera").gameObject.GetComponent<Camera>();
				if(characterCamera.pixelRect.Contains(mousePos)){
					Ray ray = characterCamera.ScreenPointToRay(mousePos);
					Debug.DrawLine( ray.origin, ray.origin + (ray.direction * 100), Color.magenta, 2 );
					RaycastHit hit;
					Physics.Raycast(ray, out hit,groundMask);
							
					character.GetComponent<AICharacterControl>().SetDestination(hit.point);
					break;

				}
			}
		}
		
	}
}
