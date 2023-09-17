using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

[BurstCompile]
public struct Pathfinding : IJob
{
    private NativeArray<HeapElement> tileMap;
    private readonly int width;
    private readonly int height;
    private readonly HeapElement startTile;
    private readonly HeapElement targetTile;
    private readonly NativeArray<HeapElement> waypoints;
    private NativeArray<int> waypointCount;
    private readonly bool noDiagonals;

    public Pathfinding(int width, int height, bool noDiagonals, NativeArray<HeapElement> tileMap, HeapElement startTile, HeapElement targetTile, NativeArray<HeapElement> waypoints, NativeArray<int> waypointCount)
    {
        this.width = width;
        this.height = height;
        this.tileMap = tileMap;
        this.startTile = startTile;
        this.targetTile = targetTile;
        this.waypoints = waypoints;
        this.waypointCount = waypointCount;
        this.noDiagonals = noDiagonals;
    }

    private void FindPath()
    {
        NativeHashSet<HeapElement> closedSet = new NativeHashSet<HeapElement>(0, Allocator.Temp);
        NativeArray<HeapElement> neighbourArray = new NativeArray<HeapElement>(8, Allocator.Temp);;
        
        int maxHeapSize = width * height;

        bool pathSuccess = false;

        if (startTile.index >= maxHeapSize || startTile.index < 0)
        {
            Debug.LogError("Start out of bounce");
            return;
        }
        if (targetTile.index >= maxHeapSize || targetTile.index < 0)
        {
            Debug.LogError("Target out of bounce");
            return;
        }

        Heap openSet = new Heap(maxHeapSize);

        openSet.Add(startTile);

        while (openSet.Size > 0)
        {
            HeapElement currentTile = openSet.Poll();
            
            closedSet.Add(currentTile);
            
            if (currentTile.index == targetTile.index)
            {
                pathSuccess = true;
                break;
                
            }
            
            CountedArray neighbours = GetNeighbours(currentTile, neighbourArray);
            
            for (int i = 0; i < neighbours.size; i++)
            {
                HeapElement neighbour = neighbours.array[i];
                
                if (!neighbour.isWalkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                int newMovementCostToNeighbour = currentTile.gCost + GetDistance(currentTile, neighbour) + neighbour.movementPenalty;

                if (newMovementCostToNeighbour >= neighbour.gCost && openSet.Contains(neighbour)) continue;
                
                neighbour.gCost = newMovementCostToNeighbour;
                neighbour.hCost = GetDistance(neighbour, targetTile);
                neighbour.parentIndex = currentTile.index;
                    
                tileMap[neighbour.index] = neighbour;

                if (!openSet.Contains(neighbour))
                {
                    openSet.Add(neighbour);
                }
                else
                {
                    
                    openSet.UpdateItem(neighbour);
                }
            }
        }

        if (pathSuccess)
        {
            RetracePath();
        }
        
        openSet.ClearHeapTracker();
        closedSet.Dispose();
        neighbourArray.Dispose();
    }

    private CountedArray GetNeighbours(HeapElement mapTile, NativeArray<HeapElement> neighbourArray)
    {
        CountedArray countedNeighbours = new CountedArray
        {
            array = neighbourArray,
            capacity = noDiagonals ? 4 : 8,
            size = 0
        };
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if(noDiagonals && (x + y == 0 || x + y == 2)) continue;
                
                if(!noDiagonals && x == 0 && y == 0) continue;
                
                Vector2Int tilePosition = IndexToPosition(mapTile.index); 
                
                int dx = tilePosition.x + x;
                int dy = tilePosition.y + y;

                if (dx < 0 || dx >= width || dy < 0 || dy >= height) continue;
                
                countedNeighbours.array[countedNeighbours.size] = GetTile(dx, dy);
                countedNeighbours.size += 1;
            }
        }
        
        return countedNeighbours;
    }

    private HeapElement GetTile(int x, int y)
    {
        return tileMap[TilePositionToIndex(x, y)];
    }
    
    private int TilePositionToIndex(int x, int y)
    {
        return width * y + x;
    }

    private Vector2Int IndexToPosition(int index)
    {
        return new Vector2Int(index % width, index / width);
    }

    private void RetracePath()
    {
        CountedArray countedWaypoints = new CountedArray
        {
            array = waypoints,
            capacity = width * height,
            size = 0
        };
        
        HeapElement currentTile = tileMap[targetTile.index];

        int safetyCounter = 0;
        const int maxSafetyCounter = 10000;
        
        while (currentTile.index != startTile.index)
        {
            if (safetyCounter >= maxSafetyCounter)
            {
                Debug.LogWarning("Retracing Path Error: Infinite Loop");
                return;
            }

            countedWaypoints.array[countedWaypoints.size] = currentTile;
            countedWaypoints.size += 1;
            
            currentTile = tileMap[currentTile.parentIndex];

            safetyCounter++;
        }
        waypointCount[0] = countedWaypoints.size;
    }

    private int GetDistance(HeapElement mapTileA, HeapElement mapTileB)
    {
        Vector2Int tileAPosition = IndexToPosition(mapTileA.index);
        Vector2Int tileBPosition = IndexToPosition(mapTileB.index);
        
        int dstX = Mathf.Abs(tileAPosition.x - tileBPosition.x);
        int dstY = Mathf.Abs(tileAPosition.y - tileBPosition.y);

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY);
        }

        return 14 * dstX + 10 * (dstY - dstX);
    }

    private struct CountedArray
    {
        public NativeArray<HeapElement> array;
        public int capacity;
        public int size;
    }

    public void Execute()
    {
        FindPath();
    }
}