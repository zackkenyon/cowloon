using Sark.Pathfinding;
using Unity.Mathematics;
using Unity.Collections;

public struct Map : IPathingMap<int3>
{
    public static int3 N = new int3(0, 0, 1);
    public static int3 S = new int3(0, 0, -1); 
    public static int3 E = new int3(1, 0, 0);
    public static int3 W = new int3(-1, 0, 0);
    public static int3 U = new int3(0, 1, 0);
    public static int3 D = new int3(0, -1, 0);

    public NativeArray<Tile> board;
    public void init()
    {
        board = new NativeArray<Tile>(512, Allocator.Persistent);
    }
    public static int idxof(int3 pos)
    {
        return pos.x + (pos.z << 3) + (pos.y << 6);
    }
    public static int3 int3ofidx(int idx)
    {
        return E * (idx & 7) + N * ((idx >> 3) & 7) + U * ((idx >> 6) & 7);
    }
    public FixedList128Bytes<int3> getNeighbors(int3 pos)
    {
        var mylist = new FixedList128Bytes<int3>();
        mylist.Add(pos + N);
        mylist.Add(pos + E);
        mylist.Add(pos + S);
        mylist.Add(pos + W);
        return mylist;
    }
    public void GetAvailableExits(int3 pos, NativeList<int3> output)
    {
        var me = board[idxof(pos)];
        var stairint = Manager.typetoint(ToPlaceType.Stair);
        if ((me.thingsIHave & stairint) != 0)
        {
            //Then I have a direction
            var dir = Manager.diroftype(ToPlaceType.Stair, me.thingOrientations);

            //TODO: Believe it or not, this is wrong 9/7/2022 The mesh thinks that the zero orientation is +z
            //0:x
            //1:-z
            //2:-x
            //3:z
            switch (dir)
            {
                case 0:
                    if (IsValidExit(pos + U + E))
                    {
                        output.Add(pos + U + E);
                    }
                    if (IsValidExit(pos + W))
                    {
                        output.Add(pos + W);
                    }
                    return;
                case 1:
                    if (IsValidExit(pos + U + S))
                    {
                        output.Add(pos + U + S);
                    }
                    if (IsValidExit(pos + N))
                    {
                        output.Add(pos + N);
                    }
                    return;
                case 2:
                    if (IsValidExit(pos + U + W))
                    {
                        output.Add(pos + U + W);
                    }
                    if (IsValidExit(pos + E))
                    {
                        output.Add(pos + E);
                    }
                    return;
                case 3:
                    if (IsValidExit(pos + U + N))
                    {
                        output.Add(pos + U + N);
                    }
                    if (IsValidExit(pos + S))
                    {
                        output.Add(pos + S);
                    }
                    return;
                default:
                    break;
            }
        }
        foreach (var neighbor in getNeighbors(pos))
        {
            //bounds checking
            if (!IsValidExit(neighbor))
            {
                continue;
            }
            //TODO: do neighbors of platforms.
            output.Add(neighbor);
        }

    }

    private bool IsValidExit(int3 neighbor)
    {
        //TOFIX: check for platforms and stairs
        return
            math.all(neighbor < 8) &&
            math.all(neighbor >= 0) && (
            neighbor.y == 0 ||
            Manager.hastype(ToPlaceType.Platform, board[Manager.indexbelow(idxof(neighbor))].thingsIHave) ||
            Manager.hastype(ToPlaceType.Stair, board[idxof(neighbor)].thingsIHave)
            );
    }

    public int GetCost(int3 a, int3 b)
    {
        //TODO: Test that stairs cost 2.
        return 1 + math.abs(a.y - b.y);
    }


    public float GetDistance(int3 a, int3 b)
    {
        return math.csum(math.abs(a - b));
    }
}
