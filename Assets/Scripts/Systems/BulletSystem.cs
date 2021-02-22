using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class BulletSystem : JobComponentSystem {

    private EntityQuery bulletGroup;
    private EntityQuery  healthGroup;

    EntityCommandBufferSystem barrier;

    private List<PrevCells> prevCells = new List<PrevCells>();

    struct PrevCells {
        public NativeMultiHashMap<int,int> damageDict;
    }

    [BurstCompile]
    struct BulletSystemJob : IJobForEachWithEntity<Bullet, Translation, Rotation> {
        public NativeMultiHashMap<int,int>.ParallelWriter damageDict;
        [ReadOnly] public PhysicsWorld physicsWorld;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float dt;
        
        public void Execute(Entity entity, int index, [ReadOnly] ref Bullet bullet, ref Translation translation, [ReadOnly] ref Rotation rotation) {
            var nextPosition = new Translation { Value = translation.Value + dt * bullet.moveSpeed * math.forward(rotation.Value) };

            CollisionFilter collisionFilter = new CollisionFilter() {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            };
            RaycastInput raycastInput = new RaycastInput {
                Start =  translation.Value,
                End =  nextPosition.Value,
                Filter = collisionFilter
            };

            if (physicsWorld.CastRay(raycastInput, out RaycastHit hit)) {
                Entity targetEntity = physicsWorld.CollisionWorld.Bodies[hit.RigidBodyIndex].Entity;
                var hash = (int)math.hash(new int2(targetEntity.Index, targetEntity.Version));
                damageDict.Add(hash, bullet.damage);
                
                commandBuffer.DestroyEntity(index, entity);
            }

            translation = nextPosition;
        }
    }

    [BurstCompile]
    struct ApplyDamage : IJobForEachWithEntity<Health> {
        [ReadOnly] public NativeMultiHashMap<int, int> damageDict;

        public void Execute(Entity entity, int index, ref Health health) {
            var hash = (int)math.hash(new int2(entity.Index, entity.Version));

            int dealtDamage = 0;
            if(damageDict.TryGetFirstValue(hash, out int item, out NativeMultiHashMapIterator<int> i)) {
                dealtDamage += item;
                while(damageDict.TryGetNextValue(out item, ref i)) {
                    dealtDamage += item;
                }
                health.Value -= dealtDamage;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies) {

        var damageDict = new NativeMultiHashMap<int,int>(bulletGroup.CalculateEntityCount(), Allocator.TempJob);

        var nextCells = new PrevCells {
            damageDict = damageDict
        };

        if (prevCells.Count == 0) {
            prevCells.Add(nextCells);
        }
        else {
            prevCells[0].damageDict.Dispose();
        }
        prevCells[0] = nextCells;

        ref PhysicsWorld physicsWorld = ref Unity.Entities.World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
        var commandBuffer = barrier.CreateCommandBuffer().ToConcurrent();

        var bulletSystemJob = new BulletSystemJob {
            damageDict = damageDict.AsParallelWriter(),
            physicsWorld = physicsWorld,
            commandBuffer = commandBuffer,
            dt = UnityEngine.Time.deltaTime,
        };
        var bulletSystemJobHandle = bulletSystemJob.Schedule(this, inputDependencies);

        barrier.AddJobHandleForProducer(bulletSystemJobHandle);

        var applyDamageJob = new ApplyDamage {
            damageDict = damageDict
        };
        var applyDamageJobHandle = applyDamageJob.Schedule(healthGroup, bulletSystemJobHandle);

        return applyDamageJobHandle;
    }

    protected override void OnCreate() {
        bulletGroup = GetEntityQuery(new EntityQueryDesc {
            All = new [] { ComponentType.ReadOnly<Bullet>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadOnly<Rotation>()},
        });
        healthGroup = GetEntityQuery(new EntityQueryDesc {
            All = new ComponentType[] { ComponentType.ReadWrite<Health>() },
        });

        barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStopRunning() {
        for (var i = 0; i < prevCells.Count; i++) {
            prevCells[i].damageDict.Dispose();
        }
        prevCells.Clear();
    }
}