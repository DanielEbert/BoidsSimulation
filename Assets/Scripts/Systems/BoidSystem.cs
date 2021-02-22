using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using Unity.Rendering;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class BoidSystem : JobComponentSystem {
    private EntityQuery  boidQuery;
    private EntityQuery  healthQuery;

    private List<Faction> uniqueFactions = new List<Faction>(10);
    private List<NativeMultiHashMap<int, int>> prevFrameHashmaps = new List<NativeMultiHashMap<int, int>>();

    EntityCommandBufferSystem m_Barrier;

    EntityArchetype bulletArchetype = Setup.bulletArchetype;

    // add a force which pulls boids/planes to the center of the map
    // so if 2 planes fight against each other, they stay in the center

    struct BulletSpawn {
        public int exists;
        public float3 position;
        public quaternion rotation;
    }

    struct Settings {
        public float CellRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float TargetWeight;
        public float MaxTargetDistance;
        //public float ObstacleAversionDistance;
        public float MoveSpeed;
        public float boidRadius;
    }

    [BurstCompile]
    struct CopyPositions : IJobForEachWithEntity<Translation> {
        public NativeArray<float3> positions;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation) {
            positions[index] = translation.Value;
        }
    }

    [BurstCompile]
    struct CopyHeadings : IJobForEachWithEntity<Rotation> {
        public NativeArray<float3> headings;

        public void Execute(Entity entity, int index, [ReadOnly]ref Rotation rotation) {
            headings[index] = math.forward(rotation.Value);
        }
    }

    [BurstCompile]
    struct CopyBoids : IJobForEachWithEntity<Boid> {
        public NativeArray<Boid> boids;

        public void Execute(Entity entity, int index, [ReadOnly]ref Boid boid) {
            boids[index] = boid;
        }
    }

    [BurstCompile]
    struct HashPositions : IJobForEachWithEntity<Translation> {
        public NativeMultiHashMap<int, int>.ParallelWriter hashMap;
        public float                                   cellRadius;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation) {
            var hash = (int)math.hash(new int3(math.floor(translation.Value / cellRadius)));
            hashMap.Add(hash, index);
        }
    }

    [BurstCompile]
    struct UpdateBoidData : IJobForEachWithEntity<Boid> {
        [ReadOnly] public NativeArray<Boid> boidsData;

        public void Execute(Entity entity, int index, ref Boid boid) {
            boid = boidsData[index];
        }
    }

    [BurstCompile]
    struct MergeCells : IJobNativeMultiHashMapMergedSharedKeyIndices {
        public NativeArray<int>                 cellIndices;
        public NativeArray<float3>              cellTargetPositions;
        public NativeArray<float3>              cellObstaclePositions;
        public NativeArray<float3>              cellAlignment;
        public NativeArray<float3>              cellSeparation;
        public NativeArray<float>               cellObstacleDistance;
        public NativeArray<int>                 cellCount;
        public NativeArray<Boid>                boidsData;
        [WriteOnly] public NativeArray<int>     killTrigger;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public Entity bulletPrefab;
        [ReadOnly] public PhysicsWorld          physicsWorld;
        public NativeMultiHashMap<int,int>.ParallelWriter damageDict;
        public NativeArray<BulletSpawn> bulletSpawns;
        //[NativeDisableParallelForRestriction] public BufferFromEntity<Damage> damageRefsFromEntity;
        public uint groupIndex;
        public float time;
        public Settings                         settings;

        // I might not want to get the closest enemy, but a specific enemy.
        // might use entity.id + entity.version of an entity and get position of entity
        // if current target is destroyed, maybe seach for new target or destroy bullet
        // add another option for auto mode
        // start with auto and maybe can focus on specifics later


        public void CheckForObstacle(int index) {
            CollisionFilter obstacleFilter = new CollisionFilter() {
                BelongsTo = ~0u,
                CollidesWith = 16u + groupIndex,
                GroupIndex = 0
            };
            PointDistanceInput obstacePointDistanceInput = new PointDistanceInput {
                Position = cellSeparation[index],
                MaxDistance = boidsData[index].obstacleAversionDistance,
                Filter = obstacleFilter
            };
            if (physicsWorld.CalculateDistance(obstacePointDistanceInput, out DistanceHit obstaceHit)) {
                cellObstaclePositions[index] = obstaceHit.Position;
                cellObstacleDistance[index] = obstaceHit.Distance;
                if (obstaceHit.Distance < settings.boidRadius) {
                    Entity targetEntity = physicsWorld.CollisionWorld.Bodies[obstaceHit.RigidBodyIndex].Entity;
                    var hash = (int)math.hash(new int2(targetEntity.Index, targetEntity.Version));
                    damageDict.Add(hash, 1);
                    killTrigger[index] = 1;
                }
            } else {
                cellObstacleDistance[index] = boidsData[index].obstacleAversionDistance + 1;
            }
        }

        // TODO: calc/find how far we gotta fly away until, the 7* dist currently is just experimental

        public void CheckForTarget(int index) {
            Boid boid = boidsData[index];

            if (boid.autoTarget == 0) {
                if (boid.timeUntilAutoTarget < time || math.distance(boid.target, cellSeparation[index]) < boid.obstacleAversionDistance) {
                    boid.autoTarget = 1;
                } else {
                    // TODO: maybe if target in front and no cooldown: shoot
                    cellTargetPositions[index] = boid.target;
                    return;
                }
            }

            CollisionFilter targetFilter = new CollisionFilter() {
                BelongsTo = ~0u,
                CollidesWith = groupIndex,
                GroupIndex = 0
            };
            PointDistanceInput targetPointDistanceInput = new PointDistanceInput {
                Position = cellSeparation[index],
                MaxDistance = settings.MaxTargetDistance,
                Filter = targetFilter
            };
            DistanceHit targetHit;
            if (!physicsWorld.CalculateDistance(targetPointDistanceInput, out targetHit)) {
                cellTargetPositions[index] = new float3(0,-1000,0);  // for me to see if sth goes wrong. idle or destroy if no enemy left
                boidsData[index] = boid;
                return;
            }
            cellTargetPositions[index] = targetHit.Position;
            if (targetHit.Distance < boid.obstacleAversionDistance) {
                boid.autoTarget = 0;
                // maybe go for 1 out of 4 positions so that the boids keep grouped
                //boid.target = (cellSeparation[index] - targetHit.Position) * 7 + cellSeparation[index];
                boid.target = math.select((cellSeparation[index] - targetHit.Position) * 7 + targetHit.Position, -(cellSeparation[index] - targetHit.Position) * 7 + targetHit.Position, ((int)cellSeparation[index][0] % 2 == 0));
                boid.timeUntilAutoTarget = time + 30;
            }
            if (boid.timeToReload > time) {
                boidsData[index] = boid;
                return;
            }
            CollisionFilter obstacleFilter = new CollisionFilter() {
                BelongsTo = ~0u,
                CollidesWith = 16u,
                GroupIndex = 0
            };
            if (physicsWorld.CastRay(new RaycastInput {Start = cellSeparation[index], End = targetHit.Position, Filter = obstacleFilter})) {
                boidsData[index] = boid;
                return;
            }
            var dirToTarget = math.dot(cellAlignment[index], math.normalizesafe(targetHit.Position - cellSeparation[index]));
            if (dirToTarget > 0.9f && targetHit.Distance < 75) {
                bulletSpawns[index] = new BulletSpawn { 
                    exists = 1, 
                    position = cellSeparation[index], 
                    rotation = quaternion.LookRotationSafe(cellAlignment[index], math.up()) // TODO: i might want dirToTarget but with a little bit of random
                };

                boid.timeToReload = time + boid.reloadTime;
            }
            
            boidsData[index] = boid;
        }

        public void ExecuteFirst(int index) {
            CheckForObstacle(index);
            CheckForTarget(index);

            cellIndices[index] = index;
        }

        public void ExecuteNext(int cellIndex, int index) {
            CheckForObstacle(index);
            CheckForTarget(index);

            cellCount[cellIndex]      += 1;
            cellAlignment[cellIndex]  = cellAlignment[cellIndex] + cellAlignment[index];
            cellSeparation[cellIndex] = cellSeparation[cellIndex] + cellSeparation[index];
            cellIndices[index]        = cellIndex;
        }
    }

    struct ApplyBulletSpawnData : IJobParallelFor {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<BulletSpawn> bulletSpawns;
        public float destroyAtTime;
        public EntityCommandBuffer.Concurrent commandBuffer;
        public Entity bulletPrefab;

        [BurstCompile]
        public void Execute(int index) {
            if (bulletSpawns[index].exists == 1) {
                Entity e = commandBuffer.Instantiate(index, bulletPrefab);
                commandBuffer.AddComponent(index, e, new Bullet{ damage = 1, moveSpeed = 100 } );
                commandBuffer.AddComponent(index, e, new DestroyAtTime { Value = destroyAtTime });
                commandBuffer.SetComponent(index, e, new Translation { Value = bulletSpawns[index].position } );
                commandBuffer.SetComponent(index, e, new Rotation { Value = bulletSpawns[index].rotation } );
            }
        }
    } 

    /*
        - make all boids(now planes) rigidboys
        - do one calcdistanceALL in close range. if none found there we do a long range check. optimization: if long range check, the whole boid group (nearby boids)
            dont need to do the same check again and copy it from the ExecuteFirst
        - do we make boids obstacles? i think no because we want to dodge (big) obstacles earlier than boids. so maybe if range to target is low (e.g. < 10) we set target to 
            the same calc as the obstacle direction is calculated

        - boid might need state. e.g. if we fly away from target because we got to close, if we turn immediately, after a 360 rotation we end at the same
            position and thus can't have time to shoot
    */

    [BurstCompile]
    struct Steer : IJobForEachWithEntity<Translation, Rotation> {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int>    cellIndices;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> targetPositions;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> obstaclePositions;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> cellAlignment;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> cellSeparation;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float>  cellObstacleDistance;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int>    cellCount;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Boid>   boidsData;
        [ReadOnly] public Settings                                        settings;
        public float                                                      dt;

        public void Execute(Entity entity, int index, ref Translation translation, ref Rotation rotation) {
            var forward                           = math.forward(rotation.Value);
            var currentPosition                   = translation.Value;
            var cellIndex                         = cellIndices[index];
            var neighborCount                     = cellCount[cellIndex];
            var alignment                         = cellAlignment[cellIndex];
            var separation                        = cellSeparation[cellIndex];
            var nearestObstacleDistance           = cellObstacleDistance[index];
            var nearestObstaclePosition           = obstaclePositions[index];
            var nearestTargetPosition             = targetPositions[index];

            var obstacleSteering                  = currentPosition - nearestObstaclePosition;
            var avoidObstacleHeading              = math.normalizesafe((nearestObstaclePosition + math.normalizesafe(obstacleSteering)
                                                    * boidsData[index].obstacleAversionDistance) - currentPosition);
            var targetHeading                     = settings.TargetWeight
                                                    * math.normalizesafe(nearestTargetPosition - currentPosition);
            //var nearestObstacleDistanceFromRadius = nearestObstacleDistance - settings.ObstacleAversionDistance;
            var alignmentResult                   = settings.AlignmentWeight
                                                    * math.normalizesafe((alignment/neighborCount)-forward);
            var separationResult                  = settings.SeparationWeight
                                                    * math.normalizesafe((currentPosition * neighborCount) - separation);
            var normalHeading                     = math.normalizesafe(alignmentResult + separationResult + targetHeading);
            var normalHeadingFactor               = math.clamp((nearestObstacleDistance - 5) / (boidsData[index].obstacleAversionDistance-5), 0, 1);
            var targetForward                     = math.normalizesafe(normalHeadingFactor * normalHeading + (1-normalHeadingFactor) * avoidObstacleHeading);
            //var targetForward                     = math.select(normalHeading, avoidObstacleHeading, nearestObstacleDistanceFromRadius < 0);
            //var nextHeading                       = math.normalizesafe(forward + 2 * dt*(targetForward-forward));   // dt limits the rotation speed

            /* var maxRot = math.PI * dt * .3f;
            var rot = math.acos(math.dot(forward, targetForward));

            // if this is used: fix the problem where it doesnt work if rot is exacty 180
            var nextHeading = math.select( 
                math.cos(maxRot) * forward + math.sin(maxRot) *  (math.normalizesafe(math.cross(math.cross(forward,targetForward), forward))),
                targetForward,
                rot <= maxRot );*/

            var rot = Quaternion.RotateTowards(rotation.Value, quaternion.LookRotationSafe(targetForward, math.mul(rotation.Value, new float3(0,1,0))), dt * 90);
            translation = new Translation{Value = new float3(currentPosition + (settings.MoveSpeed * dt * math.forward(rot)))};
            rotation = new Rotation{Value = rot};

            //physicsVelocity.Linear = new float3(nextHeading * settings.MoveSpeed);
            //rotation = new Rotation{Value = quaternion.LookRotationSafe(nextHeading, math.up())};
        }
    }

    [BurstCompile]
    struct Kill : IJobForEachWithEntity<Translation> {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> killTrigger;
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation) {
            if (killTrigger[index] != 0) {
                commandBuffer.DestroyEntity(index, entity);
            }
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
        Settings settings;
        settings.CellRadius = 16;
        settings.SeparationWeight = 1;
        settings.AlignmentWeight = 1;
        settings.TargetWeight = 2;
        settings.MaxTargetDistance = 10000;
        //settings.ObstacleAversionDistance = 35;
        settings.MoveSpeed = 25;
        settings.boidRadius = 0.5f;

        EntityManager.GetAllUniqueSharedComponentData(uniqueFactions);

        int healthCount = healthQuery.CalculateEntityCount();

        for (int i = 0; i < prevFrameHashmaps.Count; i++)
        {
            prevFrameHashmaps[i].Dispose();
        }
        prevFrameHashmaps.Clear();

        for (int index = 0; index < uniqueFactions.Count; index ++) {
            boidQuery.SetFilter(uniqueFactions[index]);

            int boidCount = boidQuery.CalculateEntityCount();

            if (boidCount == 0) continue;

            var cellIndices               = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
            var hashMap                   = new NativeMultiHashMap<int,int>(boidCount,Allocator.TempJob);
            var cellObstacleDistance      = new NativeArray<float>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
            var cellCount                 = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);
            var killTrigger               = new NativeArray<int>(boidCount, Allocator.TempJob,NativeArrayOptions.UninitializedMemory);

            var cellAlignment = new NativeArray<float3>(boidCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var cellSeparation = new NativeArray<float3>(boidCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var boidsData = new NativeArray<Boid>(boidCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var cellTargetPositions = new NativeArray<float3>(boidCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var cellObstaclePositions = new NativeArray<float3>(boidCount, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var bulletSpawns = new NativeArray<BulletSpawn>(boidCount, Allocator.TempJob, 
                NativeArrayOptions.ClearMemory);

            var damageDict = new NativeMultiHashMap<int,int>(boidCount, Allocator.TempJob);

            var initialCellAlignmentJob = new CopyHeadings {
                headings = cellAlignment
            };
            var initialCellAlignmentJobHandle = initialCellAlignmentJob.Schedule(boidQuery, inputDependencies);

            var initialCellSeparationJob = new CopyPositions {
                positions = cellSeparation
            };
            var initialCellSeparationJobHandle = initialCellSeparationJob.Schedule(boidQuery, inputDependencies);

            var initialBoidData = new CopyBoids {
                boids = boidsData
            };
            var initialBoidDataJobHandle = initialBoidData.Schedule(boidQuery, inputDependencies);

            // Cannot call [DeallocateOnJobCompletion] on Hashmaps yet
            prevFrameHashmaps.Add(hashMap);

            var hashPositionsJob = new HashPositions {
                hashMap        = hashMap.AsParallelWriter(),
                cellRadius     = settings.CellRadius
            };
            var hashPositionsJobHandle = hashPositionsJob.Schedule(boidQuery, inputDependencies);

            var initialCellCountJob = new MemsetNativeArray<int> {
                Source = cellCount,
                Value  = 1
            };
            var initialCellCountJobHandle = initialCellCountJob.Schedule(boidCount, 64, inputDependencies);

            var killTriggerJob = new MemsetNativeArray<int> {
                Source = killTrigger,
                Value  = 0
            };
            var killTriggerJobHandle = killTriggerJob.Schedule(boidCount, 64, inputDependencies);

            var initialCellBarrierJobHandle = JobHandle.CombineDependencies(initialCellAlignmentJobHandle, initialCellSeparationJobHandle, initialCellCountJobHandle);
            var initialBoidBarrierJobHandle = JobHandle.CombineDependencies(initialBoidDataJobHandle, killTriggerJobHandle);
            var mergeCellsBarrierJobHandle = JobHandle.CombineDependencies(hashPositionsJobHandle, initialCellBarrierJobHandle, initialBoidBarrierJobHandle);

            ref PhysicsWorld physicsWorld = ref Unity.Entities.World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

            var commandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent();
            prevFrameHashmaps.Add(damageDict);

            var mergeCellsJob = new MergeCells {
                cellIndices               = cellIndices,
                cellObstaclePositions     = cellObstaclePositions,
                cellTargetPositions       = cellTargetPositions,
                cellAlignment             = cellAlignment,
                cellSeparation            = cellSeparation,
                cellObstacleDistance      = cellObstacleDistance,
                cellCount                 = cellCount,
                boidsData = boidsData,
                killTrigger = killTrigger,
                physicsWorld = physicsWorld,
                damageDict = damageDict.AsParallelWriter(),
                bulletSpawns = bulletSpawns,
                commandBuffer = commandBuffer,
                bulletPrefab = BulletPrefabAuthoring.Prefab,
                //enemyEntityLook = Setup.enemyEntityLook,
                groupIndex = math.select(4u, 8u, uniqueFactions[index].Value == 0),
                time = Time.time,
                settings = settings,
            };
            var mergeCellsJobHandle = mergeCellsJob.Schedule(hashMap, 64, mergeCellsBarrierJobHandle);

            m_Barrier.AddJobHandleForProducer(mergeCellsJobHandle);

            var applyBulletSpawnDataJob = new ApplyBulletSpawnData {
                bulletSpawns = bulletSpawns,
                destroyAtTime = Time.time + 5,
                commandBuffer = commandBuffer,
                bulletPrefab = BulletPrefabAuthoring.Prefab
            };
            var applyBulletSpawnDataJobHandle = applyBulletSpawnDataJob.Schedule(boidCount, 64, mergeCellsJobHandle);

            m_Barrier.AddJobHandleForProducer(applyBulletSpawnDataJobHandle);

            var updateBoidData = new UpdateBoidData {
                boidsData = boidsData
            };
            var updateBoidDataJobHandle = updateBoidData.Schedule(boidQuery, applyBulletSpawnDataJobHandle);
            
            var steerJob = new Steer {
                cellIndices               = cellIndices,
                settings                  = settings,
                cellAlignment             = cellAlignment,
                cellSeparation            = cellSeparation,
                cellObstacleDistance      = cellObstacleDistance,
                cellCount                 = cellCount,
                targetPositions           = cellTargetPositions,
                obstaclePositions         = cellObstaclePositions,
                boidsData                 = boidsData,
                dt                        = Time.deltaTime,
            };
            var steerJobHandle = steerJob.Schedule(boidQuery, updateBoidDataJobHandle);
            
            var killJob = new Kill {
                killTrigger = killTrigger,
                commandBuffer = commandBuffer,
            };
            var killJobHandle = killJob.Schedule(boidQuery, steerJobHandle);

            m_Barrier.AddJobHandleForProducer(killJobHandle);

            var applyDamageJob = new ApplyDamage {
                damageDict = damageDict
            };
            var applyDamageJobHandle = applyDamageJob.Schedule(healthQuery, mergeCellsJobHandle);

            inputDependencies = JobHandle.CombineDependencies(killJobHandle, applyDamageJobHandle, applyBulletSpawnDataJobHandle);
            boidQuery.AddDependency(inputDependencies);
        }
        uniqueFactions.Clear();

        return inputDependencies;
    }

    protected override void OnCreate() {
        boidQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new [] { ComponentType.ReadWrite<Boid>(), ComponentType.ReadOnly<Faction>(), ComponentType.ReadWrite<Translation>(), ComponentType.ReadWrite<Rotation>()},
        });

        healthQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadWrite<Health>() },
        });

        m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStopRunning() {
        for (var i = 0; i < prevFrameHashmaps.Count; i++) {
            prevFrameHashmaps[i].Dispose();
        }
        prevFrameHashmaps.Clear();
    }
}