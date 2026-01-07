using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct SandPhysicsJob : IJob
{
    public int width;
    public int height;
    public float randomSeed;
    
    [ReadOnly] public NativeArray<Cell> readMap;
    public NativeArray<Cell> writeMap;

    public void Execute()
    {
        // Copy tá»« readMap sang writeMap vÃ  reset hasMoved flag
        for (int i = 0; i < readMap.Length; i++)
        {
            writeMap[i] = readMap[i];
            var cell = writeMap[i];
            cell.hasMoved = false;
            writeMap[i] = cell;
        }

        // QuÃ©t tá»« DÆ¯á»šI lÃªn TRÃŠN Ä‘á»ƒ xá»­ lÃ½ gravity Ä‘Ãºng
        var halfWidth = width / 2;
        for (int y = 1; y < height; y++)
        {
            for (int x = 0; x < halfWidth; x++)
            {
                var idx = y * width + x;
                Cell cell = writeMap[idx];
                if (cell.type == 1)
                {
                    var downIdx = (y - 1) * width + x;
                    
                    // Check straight down FIRST - most natural behavior


                    // Only check diagonals if straight down is blocked
                    // Check down-right (only if not at right edge)
                    if (x < width - 1)
                    {
                        var downRightIdx = (y - 1) * width + (x + 1);
                        if (writeMap[downRightIdx].type == 0)
                        {
                            MoveCell(idx, downRightIdx);
                            continue;
                        }
                    }

                    // Check down-left (only if not at left edge)
                    if (x > 0)
                    {
                        var downLeftIdx = (y - 1) * width + (x - 1);
                        if (writeMap[downLeftIdx].type == 0)
                        {
                            MoveCell(idx, downLeftIdx);
                            continue;
                        }
                    }
                    
                    if (writeMap[downIdx].type == 0)
                    {
                        MoveCell(idx, downIdx);
                        continue;
                    }
                }
            }

            for (int x = width - 1; x >= halfWidth; x--)
            {
                var idx = y * width + x;
                Cell cell = writeMap[idx];
                if (cell.type == 1)
                {

                    var downIdx = (y - 1) * width + x;



                    // Only check diagonals if straight down is blocked
                    // Check down-left (only if not at left edge)
                    if (x > 0)
                    {
                        var downLeftIdx = (y - 1) * width + (x - 1);
                        if (writeMap[downLeftIdx].type == 0)
                        {
                            MoveCell(idx, downLeftIdx);
                            continue;
                        }
                    }

                    // Check down-right (only if not at right edge)
                    if (x < width - 1)
                    {
                        var downRightIdx = (y - 1) * width + (x + 1);
                        if (writeMap[downRightIdx].type == 0)
                        {
                            MoveCell(idx, downRightIdx);
                            continue;
                        }
                    }
                    
                    // Check straight down FIRST - most natural behavior
                    if (writeMap[downIdx].type == 0)
                    {
                        MoveCell(idx, downIdx);
                        continue;
                    }
                }
            }
        }
    }

    void MoveCell(int fromIdx, int toIdx)
    {
        if (writeMap[toIdx].type == 0)
        {
            writeMap[toIdx] = writeMap[fromIdx];
            var empty = new Cell { type = 0, color = new Color32(0,0,0,0) };
            writeMap[fromIdx] = empty;
        }
    }
}