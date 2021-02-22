using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BulletPrefabAuthoring : MonoBehaviour, IConvertGameObjectToEntity {

    public static Entity Prefab;

    public void Convert(Entity entity, EntityManager em, GameObjectConversionSystem conversionSystem) {
        em.AddSharedComponentData<BulletPrefab>(entity, new BulletPrefab());
        Prefab = entity;
    }
}
