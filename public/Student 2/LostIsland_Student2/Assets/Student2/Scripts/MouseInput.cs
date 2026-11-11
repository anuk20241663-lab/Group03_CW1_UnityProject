using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInput : MonoBehaviour
{
	//PSEUDO CODE: Declare public variable of type float for speed;
	public float speed = 10f;
	void Update()
	{

	}
	void OnMouseDrag()
	{
		//TRY:
		// Declare and Initialize the X and Y mouse axes and multiply by speed.
		// The value is in the range -1 to 1
        //Mouse Y will become Z translation

		// Make translation variable move at 10 meters per second instead of 10 meters per frame with TimeDeltaTime...

		// use transform to translate with values above

		//Try Stretch Task in tutorial sheet
		float mouseX = Input.GetAxis("Mouse X") * speed * Time.deltaTime;
		float mouseZ = Input.GetAxis("Mouse Y") * speed * Time.deltaTime;

		transform.Translate(mouseX, 0f, mouseZ, Space.World);
		
	}
}

//From: https://docs.unity3d.com/ScriptReference/Input.GetAxis.html
