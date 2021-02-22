using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;


public class KillHealthObjectSystem : JobComponentSystem {

    EntityCommandBufferSystem m_Barrier;

    [BurstCompile]
    struct KillHealthObjectSystemJob : IJobForEachWithEntity<Health> {
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, ref Health health) {
            if (health.Value <= 0) {
                commandBuffer.DestroyEntity(index, entity);
            }
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        var job = new KillHealthObjectSystemJob{
            commandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent()
        };
        var jobHandle = job.Schedule(this, inputDependencies);
        m_Barrier.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }

    protected override void OnCreate() {
        m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
}