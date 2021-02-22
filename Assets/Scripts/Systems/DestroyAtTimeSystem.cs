using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class DestroyAtTimeSystem : JobComponentSystem {

    EntityCommandBufferSystem m_Barrier;

    [BurstCompile]
    struct DestroyAtTimeSystemJob : IJobForEachWithEntity<DestroyAtTime> {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float time;
        
        public void Execute(Entity entity, int index, [ReadOnly] ref DestroyAtTime destroyAtTime) {
            if (time >= destroyAtTime.Value) {
                commandBuffer.DestroyEntity(index, entity);
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new DestroyAtTimeSystemJob {
            commandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent(),
            time = Time.time
        };
        var jobHandle = job.Schedule(this, inputDependencies);

        m_Barrier.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }

    protected override void OnCreate() {
        m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
}