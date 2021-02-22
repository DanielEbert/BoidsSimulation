using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Bullet : IComponentData {
    public int damage;
    public float moveSpeed;
}
