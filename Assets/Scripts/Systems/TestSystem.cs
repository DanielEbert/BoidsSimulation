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
public class TestSystem : JobComponentSystem {

    // looks like i cant use burst because of hits.dispose. performance shoud not matter too much because we only do it once every X seconds (e.g. every 3 seconds or when target died)
    // if its too big maybe we can do the dispose in another extra job
    //[BurstCompile]
    struct TestSystemJob : IJobForEach<Translation> {
        [ReadOnly] public PhysicsWorld physicsWorld;
        
        public void Execute(ref Translation translation) {
            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);

            CollisionFilter targetFilter = new CollisionFilter() {
                BelongsTo = ~0u,
                CollidesWith = 8u,
                GroupIndex = 0
            };

            var pointDistanceInput = new PointDistanceInput {
                Position = translation.Value,
                MaxDistance = 50,
                Filter = targetFilter
            };

            physicsWorld.CalculateDistance(pointDistanceInput, ref hits);

            hits.Dispose();
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies) {

        return inputDependencies;
        
        /* ref PhysicsWorld physicsWorld = ref Unity.Entities.World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

        var job = new TestSystemJob() {
            physicsWorld = physicsWorld
        };
        
        // Assign values to the fields on your job here, so that it has
        // everything it needs to do its work when it runs later.
        // For example,
             
        
        
        
        // Now that the job is set up, schedule it to be run. 
        return job.Schedule(this, inputDependencies);*/
    }
}