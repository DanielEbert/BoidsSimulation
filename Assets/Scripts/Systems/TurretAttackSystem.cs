using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BoidSystem))]
public class TurretAttackSystem : JobComponentSystem {

    private EntityQuery turretGroup;
    private EntityQuery boidGroup;

    private List<PrevData> prevData = new List<PrevData>();

    EntityCommandBufferSystem m_Barrier;

    struct PrevData {
        public NativeMultiHashMap<int, int> boidDict;
        public NativeArray<float3> boidPosition;
        public NativeHashMap<int, int> turretTarget;
    }

    /* struct TurretTarget {
        public NativeArray<int> index;  // index is -1 if no target
        public NativeArray<float3> position;
    }*/

    [BurstCompile]
    struct CopyPositions : IJobForEachWithEntity<Translation> {
        public NativeArray<float3> positions;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation)
        {
            positions[index] = translation.Value;
        }
    }

    [BurstCompile]
    struct HashPositions : IJobForEachWithEntity<Translation> {
        public NativeMultiHashMap<int, int>.ParallelWriter boidDict;
        public float                                   cellRadius;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation) {
            var hash = (int)math.hash(new int3(math.floor(translation.Value / cellRadius)));
            boidDict.Add(hash, index);
        }
    }

    [BurstCompile]
    struct AttackClosestTarget : IJobForEachWithEntity<Translation, Rotation, Turret> {
        [ReadOnly] public NativeMultiHashMap<int, int> boidDict;
        [ReadOnly] public NativeArray<float3> boidPosition;
        //[ReadOnly] public NativeArray<float3> currentRotation;
        //[WriteOnly] public NativeArray<float3> nextRotation;
        public NativeHashMap<int, int>.ParallelWriter turretTarget;
        [ReadOnly] public PhysicsWorld          physicsWorld;
        public float cellRadius;
        public float dt;
        public float time;

        // skip the turrets where we still reload
        // raycast currently hits itself. if raycast is shot outwards shoudnt it skip itself?
        // A could be 1) to add turret diameter/extend to Target to RaycastInput Start or 2) make the
        // turret itself not have a collider

        // TODO: turret range in Turret Component

        int GetClosestTarget(float3 position, float turretRange) {
            float curClosestDist = turretRange;
            int curIndex = -1;
            int extend = (int)(turretRange / cellRadius) + 1;

            var boidDictIndex = math.floor(position / cellRadius);

            for(int z = -extend; z <= extend; z++) {
                for(int y = -extend; y <= extend; y++) {
                    for(int x = -extend; x <= extend; x++) {
                        var hash = (int)math.hash(new int3(boidDictIndex + new int3(x,y,z)));
                        NativeMultiHashMapIterator<int> i;
                        if(boidDict.TryGetFirstValue(hash, out int item, out i)) {
                            float dst = math.distance(boidPosition[item], position);
                            if (dst < curClosestDist) {
                                var raycastInput = new RaycastInput {
                                    Start = position,
                                    End = boidPosition[item],
                                    Filter = CollisionFilter.Default
                                };
                                if (!physicsWorld.CastRay(raycastInput)) {
                                    curClosestDist = dst;
                                    curIndex = item;
                                }
                            }
                            while(boidDict.TryGetNextValue(out item, ref i)) {
                                dst = math.distance(boidPosition[item], position);
                                if (dst < curClosestDist) {
                                    var raycastInput = new RaycastInput {
                                        Start = position,
                                        End = boidPosition[item],
                                        Filter = CollisionFilter.Default
                                    };
                                    if (!physicsWorld.CastRay(raycastInput)) {
                                        curClosestDist = dst;
                                        curIndex = item;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return curIndex;
        }

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation, ref Rotation rotation, ref Turret turret) {
            int ind = GetClosestTarget(translation.Value, turret.attackRange);

            if (ind == -1) return;

            // this rotates towards blue vector which is the forward vector in unity
            var forward = math.forward(rotation.Value);
            var toTarget = math.forward(quaternion.LookRotationSafe(boidPosition[ind] - translation.Value, math.up()));
            var q = quaternion.LookRotationSafe( forward + dt * 2 * toTarget, math.up());
            rotation = new Rotation{ Value = q };
            
            var radianToTargetRotation = math.acos(math.dot(math.forward(q), toTarget));

            if (radianToTargetRotation < 0.174f && time >= turret.reloadTime) {
                turret.reloadTime = time + turret.timeToReload;
                turretTarget.TryAdd(ind, index);
            }
        }
    }

    [BurstCompile]
    struct Kill : IJobForEachWithEntity<Translation> {
        [ReadOnly] public NativeHashMap<int, int> turretTarget;
        [WriteOnly] public EntityCommandBuffer.Concurrent commandBuffer;


        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation) {
            if (turretTarget.TryGetValue(index, out int item)) {
                commandBuffer.DestroyEntity(index, entity);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        int cellRadius = 50;

        var boidCount = boidGroup.CalculateEntityCount();
        var turretCount = turretGroup.CalculateEntityCount();

        if (boidCount == 0) {
            // TODO: or maybe rotate to idle
            return inputDependencies;
        }

        var boidPosition = new NativeArray<float3>(boidCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
        var boidDict = new NativeMultiHashMap<int,int>(boidCount, Allocator.TempJob);
        var turretTarget = new NativeHashMap<int, int>(turretCount, Allocator.TempJob);
        //var turretTarget = new NativeArray<TurretTarget>(turretCount, Allocator.TempJob);

        var nextData = new PrevData {
            boidDict = boidDict,
            boidPosition = boidPosition,
            turretTarget = turretTarget
        };
        
        // Data Cleanup
        if (prevData.Count != 0) {
            prevData[0].boidDict.Dispose();
            prevData[0].boidPosition.Dispose();
            prevData[0].turretTarget.Dispose();
        } else {
            prevData.Add(nextData);
        }
        prevData[0] = nextData;

        var copyBoidPositionsJob = new CopyPositions {
            positions = boidPosition
        };
        var copyBoidPositionsJobHandle = copyBoidPositionsJob.Schedule(boidGroup, inputDependencies);

        var hashPositionsJob = new HashPositions {
            boidDict       = boidDict.AsParallelWriter(),
            cellRadius     = cellRadius
        };
        var hashPositionsJobHandle = hashPositionsJob.Schedule(boidGroup, copyBoidPositionsJobHandle);

        ref PhysicsWorld physicsWorld = ref Unity.Entities.World.Active.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>().PhysicsWorld;

        var AttackClosestTargetJob = new AttackClosestTarget {
            boidDict = boidDict,
            boidPosition = boidPosition,
            turretTarget = turretTarget.AsParallelWriter(),
            physicsWorld = physicsWorld,
            cellRadius     = cellRadius,
            dt = Time.deltaTime,
            time = Time.time
        };
        var AttackClosestTargetJobHandle = AttackClosestTargetJob.Schedule(turretGroup, hashPositionsJobHandle);

        var commandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent();

        var killJob = new Kill {
            turretTarget = turretTarget,
            commandBuffer = commandBuffer,
        };
        var killJobHandle = killJob.Schedule(boidGroup, AttackClosestTargetJobHandle);

        m_Barrier.AddJobHandleForProducer(killJobHandle);

        return killJobHandle;
    }

    protected override void OnCreate() {
        turretGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new [] { ComponentType.ReadWrite<Turret>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadWrite<Rotation>()},
        });

        boidGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new [] { ComponentType.ReadOnly<Boid>(), ComponentType.ReadOnly<Translation>()},
        });

        m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStopRunning() {
        for (var i = 0; i < prevData.Count; i++) {
            prevData[0].boidDict.Dispose();
            prevData[0].boidPosition.Dispose();
            prevData[0].turretTarget.Dispose();
        }
        prevData.Clear();
    }
}  
