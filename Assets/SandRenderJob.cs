using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct SandRenderJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Cell> mapData;
    public NativeArray<Color32> textureOut;

    public void Execute(int index)
    {
        if (mapData[index].type == 0)
            textureOut[index] = new Color32(0, 0, 0, 0); // Transparent
        else
            textureOut[index] = mapData[index].color;
    }
}
