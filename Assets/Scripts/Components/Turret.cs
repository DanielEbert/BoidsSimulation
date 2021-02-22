using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Turret : IComponentData {
    public float reloadTime;
    public float timeToReload;
    public float attackRange;
}
