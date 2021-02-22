using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour {
    public Transform t;

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = t.position;
    }
}
