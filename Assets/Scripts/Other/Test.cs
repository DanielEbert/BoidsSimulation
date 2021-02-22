using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour {

    public Transform t;
    
    PlayerControls controls;

    Vector2 rotate;
    Vector2 rotateCam;
    bool accelerate;
    bool breaking;

    Vector3 rot;

    void Awake() {
        rot = transform.rotation.eulerAngles;

        controls = new PlayerControls();

        controls.Gameplay.Rotate.performed += ctx => rotate = ctx.ReadValue<Vector2>();
        controls.Gameplay.Rotate.canceled += ctx => rotate = Vector2.zero;

        controls.Gameplay.RotateCam.performed += ctx => rotateCam = ctx.ReadValue<Vector2>();
        controls.Gameplay.RotateCam.canceled += ctx => rotateCam = Vector2.zero;

        controls.Gameplay.Accelerate.performed += ctx => Acceleration(true);
        controls.Gameplay.Accelerate.canceled += ctx => Acceleration(false);

        controls.Gameplay.Break.performed += ctx => Breaking(true);
        controls.Gameplay.Break.canceled += ctx => Breaking(false);
    }

    void Acceleration(bool b) {
        accelerate = b;
    }

    void Breaking(bool b) {
        breaking = b;
    }

    void Update() {
        if (accelerate) {
            transform.position += transform.forward * Time.deltaTime * 30;
        }
        if (breaking) {
            transform.position -= transform.forward * Time.deltaTime * 30;
        }
        //transform.Rotate(30f * Time.deltaTime * new Vector2(-rotate.y, rotate.x));
        /*rot.x += 50f * Time.deltaTime * -rotate.y;

        if(Vector3.Dot(Vector3.up, transform.up) >= 0) {
            rot.y += 50f * Time.deltaTime * rotate.x;
        } else {
            rot.y -= 50f * Time.deltaTime * rotate.x;
        }
        
        transform.rotation = Quaternion.Euler(rot);*/

        Vector2 r = new Vector2(0, rotate.y >= 0 ? rotate.x : -rotate.x) * 50f * Time.deltaTime;
        transform.Rotate(r, Space.Self);

        t.Rotate(new Vector2(0, rotateCam.x) * 120f * Time.deltaTime, Space.Self);

        if (rotate.y > 0)
            transform.position += transform.forward * Time.deltaTime * rotate.y * 10;
        else 
            transform.position -=   transform.forward * Time.deltaTime * -rotate.y * 10;

    }

    void OnEnable() {
        controls.Gameplay.Enable();
    }

    void OnDisable() {
        controls.Gameplay.Disable();
    }
}
