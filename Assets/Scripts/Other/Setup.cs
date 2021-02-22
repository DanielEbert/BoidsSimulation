//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;
using Unity;
using Unity.Burst;

public class Setup {

    static int startAllyCount = 500;
    static int startEnemyCount = 500;
    public static EntityArchetype boidArchetype;
    public static EntityArchetype enemyArchetype;
    public static EntityArchetype turretArchetype;
    public static EntityArchetype bulletArchetype;
    public static RenderMesh allyEntityLook;
    public static RenderMesh enemyEntityLook;

    public static Unity.Mathematics.Random random = new Unity.Mathematics.Random(234890);
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] 
    public static void InitializeBeforeScene() {
        var em = World.Active.EntityManager;
        CreateArchtypes(em);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeAfterScene() {
        var em = World.Active.EntityManager;
        allyEntityLook = new RenderMesh {
            mesh = GameObject.Find("Manager").GetComponent<Utility>().entityLookMesh,
            material = GameObject.Find("Manager").GetComponent<Utility>().entityLookMaterial
        };
        enemyEntityLook = new RenderMesh {
            mesh = GameObject.Find("Manager").GetComponent<Utility>().entityLookMesh,
            material = GameObject.Find("Manager").GetComponent<Utility>().enemyEntityLookMaterial
        };
        CreateBoidEntities(em, startAllyCount, startEnemyCount);
        //CreateEnemyEntities(em, 1);
        //CreateTurretEntities(em, 2);
        CreateBulletEntities(em, 1);
    }

    private static void CreateArchtypes(EntityManager em) {
        boidArchetype = em.CreateArchetype( new ComponentType[]  {
                new ComponentType(typeof(Translation)),
                new ComponentType(typeof(Rotation)),
                new ComponentType(typeof(LocalToWorld)),
                new ComponentType(typeof(Boid)),
                //new ComponentType(typeof(PhysicsCollider)),
            }
        );

        enemyArchetype = em.CreateArchetype( new ComponentType[] {
                new ComponentType(typeof(Translation)),
                new ComponentType(typeof(Rotation)),
                new ComponentType(typeof(LocalToWorld)),
                new ComponentType(typeof(PhysicsCollider)),
                new ComponentType(typeof(Health)),
            }
        );

        turretArchetype = em.CreateArchetype( new ComponentType[] {
                new ComponentType(typeof(Translation)),
                new ComponentType(typeof(Rotation)),
                new ComponentType(typeof(LocalToWorld)),
                new ComponentType(typeof(Turret)),
            }
        );

        bulletArchetype = em.CreateArchetype (new ComponentType[] {
                new ComponentType(typeof(Bullet)),
                new ComponentType(typeof(Translation)),
                new ComponentType(typeof(Rotation)),
                new ComponentType(typeof(LocalToWorld)),
            }
        );
    }

    [BurstCompile]
    private unsafe static void CreateBoidEntities(EntityManager em, int count, int count2) {
        NativeArray<Entity> entities = new NativeArray<Entity>(count + count2, Allocator.Temp);
        em.CreateEntity(boidArchetype, entities);

        CollisionFilter allyCollisionFilter = new CollisionFilter() {
            BelongsTo = 4u,
            CollidesWith = ~0u,
            GroupIndex = 0
        };

        CollisionFilter enemyCollisionFilter = new CollisionFilter() {
            BelongsTo = 8u,
            CollidesWith = ~0u,
            GroupIndex = 0
        };

        BlobAssetReference<Unity.Physics.Collider> allySpCollider = Unity.Physics.SphereCollider.Create(float3.zero, 0.5f, allyCollisionFilter);
        BlobAssetReference<Unity.Physics.Collider> enemySpCollider = Unity.Physics.SphereCollider.Create(float3.zero, 0.5f, enemyCollisionFilter);

        for (int i = 0; i < count; i++) {
            em.AddSharedComponentData<RenderMesh>(entities[i], allyEntityLook);
            em.AddSharedComponentData<Faction>(entities[i], new Faction { Value = 0 });
            em.SetComponentData(entities[i], new Boid { autoTarget = 1, reloadTime = 1, timeToReload = 0, obstacleAversionDistance = random.NextFloat(40, 50) });
            em.SetComponentData(entities[i], new Translation { Value = new float3(random.NextFloat(-100, 100), random.NextFloat(-100, 100), random.NextFloat(-100, 100)) });
            em.SetComponentData(entities[i], new Rotation { Value = Quaternion.identity });
            //em.SetComponentData(entities[i], new PhysicsCollider { Value = allySpCollider });
        }

        for (int i = count; i < entities.Length; i++) {
            em.AddSharedComponentData<RenderMesh>(entities[i], enemyEntityLook);
            em.AddSharedComponentData<Faction>(entities[i], new Faction { Value = 0 });
            em.SetComponentData(entities[i], new Boid { autoTarget = 1, reloadTime = 1, timeToReload = 0, obstacleAversionDistance = random.NextFloat(40, 50) });
            em.SetComponentData(entities[i], new Translation { Value = new float3(random.NextFloat(-100, 100), random.NextFloat(-100, 100), random.NextFloat(-100, 100)) });
            em.SetComponentData(entities[i], new Rotation { Value = Quaternion.identity });
            //em.SetComponentData(entities[i], new PhysicsCollider { Value = enemySpCollider });
        }

        entities.Dispose();
    }

    [BurstCompile]
    private unsafe static void CreateEnemyEntities(EntityManager em, int count) {
        NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.CreateEntity(enemyArchetype, entities);

        CollisionFilter filter = new CollisionFilter() {
            BelongsTo = 8u,
            CollidesWith = ~0u,
            GroupIndex = 0
        };

        BlobAssetReference<Unity.Physics.Collider> spCollider = Unity.Physics.SphereCollider.Create(float3.zero, 1f, filter);

        for (int i = 0; i < count; i++) {
            em.AddSharedComponentData<RenderMesh>(entities[i], enemyEntityLook);
            //.SetComponentData(entities[i], new LocalToWorld{Value = float4x4.TRS(
            //            new float3(random.NextFloat(-100, 100), random.NextFloat(-100, 100), random.NextFloat(-100, 100)),
            //            quaternion.identity,
            //            new float3(1.0f, 1.0f, 1.0f))});
            em.SetComponentData(entities[i], new Translation { Value = new float3(1000, 0, 0) });
            em.SetComponentData(entities[i], new Rotation { Value = Quaternion.identity });
            em.SetComponentData(entities[i], new PhysicsCollider { Value = spCollider });
            em.SetComponentData(entities[i], new Health { Value = 100 });
            //em.SetComponentData(entities[i], new Turret { attackRange = 50, reloadTime = 1, timeToReload = 0 });
            //Unity.Physics.Collider* colliderPtr = (Unity.Physics.Collider*)spCollider.GetUnsafePtr();
            //em.SetComponentData(entities[i], PhysicsMass.CreateDynamic(colliderPtr->MassProperties, 1));

            /* em.SetComponentData(entities[i], new PhysicsVelocity()
            {
                Linear = 0,
                Angular = 0
            });*/
            //em.SetComponentData(entities[i], new PhysicsDamping()
            //{
            //    Linear = 0.01f,
            //    Angular = 0.05f
            //});
            //em.SetComponentData(entities[i], new PhysicsGravityFactor() { Value = 0f });
        }
        entities.Dispose();
    }

    [BurstCompile]
    private unsafe static void CreateTurretEntities(EntityManager em, int count) {
        NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.CreateEntity(turretArchetype, entities);

        em.AddSharedComponentData<RenderMesh>(entities[0], enemyEntityLook);
        em.SetComponentData(entities[0], new Translation { Value = new float3(-500, 0, 55) });
        em.SetComponentData(entities[0], new Rotation { Value = Quaternion.identity });
        em.SetComponentData(entities[0], new Turret { attackRange = 125, timeToReload = 1, reloadTime = 0 });

        em.AddSharedComponentData<RenderMesh>(entities[1], enemyEntityLook);
        em.SetComponentData(entities[1], new Translation { Value = new float3(-500, 0, -55) });
        em.SetComponentData(entities[1], new Rotation { Value = Quaternion.identity });
        em.SetComponentData(entities[1], new Turret { attackRange = 125, timeToReload = 1, reloadTime = 0 });

        /* for (int i = 0; i < count; i++) {
            em.AddSharedComponentData<RenderMesh>(entities[i], enemyEntityLook);
            em.SetComponentData(entities[i], new Translation { Value = new float3(-1000, 0, 0) });
            em.SetComponentData(entities[i], new Rotation { Value = Quaternion.identity });
            em.SetComponentData(entities[i], new Turret { attackRange = 50, reloadTime = 1, timeToReload = 0 });
        }*/
        entities.Dispose();
    }

    [BurstCompile]
    private unsafe static void CreateBulletEntities(EntityManager em, int count) {
        NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp);
        em.CreateEntity(bulletArchetype, entities);

        CollisionFilter filter = new CollisionFilter() {
            BelongsTo = ~0u,
            CollidesWith = ~0u,
            GroupIndex = 0
        };

        for (int i = 0; i < count; i++) {
            em.AddSharedComponentData<RenderMesh>(entities[i], enemyEntityLook);
            //.SetComponentData(entities[i], new LocalToWorld{Value = float4x4.TRS(
            //            new float3(random.NextFloat(-100, 100), random.NextFloat(-100, 100), random.NextFloat(-100, 100)),
            //            quaternion.identity,
            //            new float3(1.0f, 1.0f, 1.0f))});
            em.SetComponentData(entities[i], new Bullet { damage = 1, moveSpeed = 100 });
            em.SetComponentData(entities[i], new Translation { Value = new float3(500, 0, -1000) });
            em.SetComponentData(entities[i], new Rotation { Value = Quaternion.identity });
            //em.SetComponentData(entities[i], new Turret { attackRange = 50, reloadTime = 1, timeToReload = 0 });
            //Unity.Physics.Collider* colliderPtr = (Unity.Physics.Collider*)spCollider.GetUnsafePtr();
            //em.SetComponentData(entities[i], PhysicsMass.CreateDynamic(colliderPtr->MassProperties, 1));

            /* em.SetComponentData(entities[i], new PhysicsVelocity()
            {
                Linear = 0,
                Angular = 0
            });*/
            //em.SetComponentData(entities[i], new PhysicsDamping()
            //{
            //    Linear = 0.01f,
            //    Angular = 0.05f
            //});
            //em.SetComponentData(entities[i], new PhysicsGravityFactor() { Value = 0f });
        }
        entities.Dispose();
    }
}
