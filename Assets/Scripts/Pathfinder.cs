using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sark.Pathfinding;
using Unity.Mathematics;
using Unity.Collections;

public class Pathfinder : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AStar<int3> myastar = new AStar<int3>(100, Unity.Collections.Allocator.Persistent);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

struct MyMap : IPathingMap<int3>
{
    public void GetAvailableExits(int3 pos, NativeList<int3> output)
    {
        throw new System.NotImplementedException();
    }

    public int GetCost(int3 a, int3 b)
    {
        return 1 + math.abs(a.y - b.y);
    }

    public float GetDistance(int3 a, int3 b)
    {
        return math.csum(a - b);
    }
}