using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Rendering;

public class Utility : MonoBehaviour {

    public static Utility Instance;

    public Mesh entityLookMesh;
    public Material entityLookMaterial;
    public Material enemyEntityLookMaterial;

    void Awake() {
        Instance = this;
    }
}
