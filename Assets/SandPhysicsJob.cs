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
        // Copy từ readMap sang writeMap và reset hasMoved flag
        for (int i = 0; i < readMap.Length; i++)
        {
            writeMap[i] = readMap[i];
            var cell = writeMap[i];
            cell.hasMoved = false;
            writeMap[i] = cell;
        }

        // Quét từ DƯỚI lên TRÊN để xử lý gravity đúng
        Unity.Mathematics.Random rnd = new Unity.Mathematics.Random((uint)(randomSeed * 1000 + 1));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                Cell current = readMap[idx];

                if (current.type == 1) // Nếu là CÁT
                {
                    // Check bên dưới
                    if (y > 0)
                    {
                        int downIdx = (y - 1) * width + x;
                        int downLeftIdx = (y - 1) * width + (x - 1);
                        int downRightIdx = (y - 1) * width + (x + 1);

                        // 1. Rơi thẳng
                        if (writeMap[downIdx].type == 0) // Air
                        {
                            MoveCell(idx, downIdx);
                        }
                        // 2. Tương tác với BĂNG CHUYỀN
                        else if (writeMap[downIdx].type == 3) // Conveyor Right
                        {
                            if (x < width - 1 && writeMap[idx + 1].type == 0)
                                MoveCell(idx, idx + 1);
                        }
                        else if (writeMap[downIdx].type == 4) // Conveyor Left
                        {
                            if (x > 0 && writeMap[idx - 1].type == 0)
                                MoveCell(idx, idx - 1);
                        }
                        // 3. Trượt khi gặp vật cản
                        else 
                        {
                            bool goLeft = rnd.NextBool();
                            if (goLeft)
                            {
                                if (x > 0 && writeMap[downLeftIdx].type == 0) MoveCell(idx, downLeftIdx);
                                else if (x < width - 1 && writeMap[downRightIdx].type == 0) MoveCell(idx, downRightIdx);
                            }
                            else
                            {
                                if (x < width - 1 && writeMap[downRightIdx].type == 0) MoveCell(idx, downRightIdx);
                                else if (x > 0 && writeMap[downLeftIdx].type == 0) MoveCell(idx, downLeftIdx);
                            }
                        }
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
