using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Faction : ISharedComponentData {
    public int Value;
}
