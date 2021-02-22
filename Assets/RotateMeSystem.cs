using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RotateMeSystem : JobComponentSystem {
    [BurstCompile]
    struct RotateMeJob : IJobForEach<RotateMe, Translation, Rotation> {
        public float dt;
        
        public void Execute([ReadOnly] ref RotateMe r, ref Translation translation, ref Rotation rotation) {
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(new float3(1,0,0) / 2, dt));
            translation = new Translation{ Value = translation.Value + math.mul(rotation.Value, new float3(0, 1, 0)) * dt * 10};
        }
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var rotateMeJob = new RotateMeJob {
            dt = UnityEngine.Time.deltaTime,
        };
        return rotateMeJob.Schedule(this, inputDependencies);
    }
}