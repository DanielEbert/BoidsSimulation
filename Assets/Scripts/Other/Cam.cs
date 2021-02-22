using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour
{
    Vector2 rotation = new Vector2 (0, 0);
	public float rotationSpeed = 3;
    public float movementSpeed = 200;

	void Update () {
		rotation.y += Input.GetAxis ("Mouse X");
		rotation.x += -Input.GetAxis ("Mouse Y");
		transform.eulerAngles = (Vector2)rotation * rotationSpeed;

        if (Input.GetKey(KeyCode.W)) {
            transform.position += transform.forward * Time.deltaTime * movementSpeed;
        } 
        if (Input.GetKey(KeyCode.S)) {
            transform.position -= transform.forward * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.A)) {
            transform.position -= transform.right * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.D)) {
            transform.position += transform.right * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.Space)) {
            transform.position += transform.up * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            transform.position -= transform.up * Time.deltaTime * movementSpeed;
        }
	}
}
