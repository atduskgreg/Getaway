using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityStandardAssets.CrossPlatformInput;
using LOS;

public class LookAtMouse : MonoBehaviour {
	Camera characterCamera;
	Camera aimCamera;

	float speed = 5f;



	void Start () {
		characterCamera = transform.FindChild("Camera").GetComponent<Camera>();
		aimCamera = gameObject.transform.FindChild("aim").GetComponent<Camera>();
	}
	
	void Update () {
		
	}

	public void DoLook(){
		if(!GetComponent<AICharacterControl>().isMoving){
			print(new Vector2(Input.mousePosition.x - characterCamera.pixelRect.min.x, Input.mousePosition.y - characterCamera.pixelRect.min.y));

			Ray ray = characterCamera.ScreenPointToRay(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
			Debug.DrawLine( ray.origin, ray.origin + (ray.direction * 100), Color.magenta, 2 );


			Quaternion targetRotation = Quaternion.LookRotation(ray.direction, Vector3.up);
			// TODO: contrain rotation to only rotate around the y-axis and apply it to the model instead of the aim
//			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
			aimCamera.gameObject.transform.rotation = Quaternion.Slerp(aimCamera.gameObject.transform.rotation, targetRotation, speed * Time.deltaTime);


		}
	}
}
