using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Sark.Pathfinding;
using Unity.Mathematics;
using Unity.Collections;

public enum ToPlaceType
{
    Platform,
    Stair,
    Bed,
    BigTrough,
    LittleTrough,
    Baby
}
[Serializable]
public struct Tile
{
    public List<GameObject> things;
    public int thingsIHave;
    public int thingOrientations;
}
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
    public int idxof(int3 pos)
    {
        return pos.x + pos.z << 3 + pos.y << 6;
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


        }
    }

    private bool IsValidExit(int3 neighbor)
    {
        //TOFIX: check for platforms and stairs
        return math.all(neighbor < 8) && math.all(neighbor >= 0);
    }

    public int GetCost(int3 a, int3 b)
    {
        //TODO: Make sure that stairs cost 2.
        return 1 + math.abs(a.y - b.y);
    }


    public float GetDistance(int3 a, int3 b)
    {
        return math.csum(math.abs(a - b));
    }
}


public class Manager : MonoBehaviour
{
    public static Manager Instance;
    public Transform[] prefabs;
    public Transform[] ghostPrefabs;
    public Transform toPlace;
    public int toPlaceNum;
    public TMPro.TextMeshProUGUI Currenttextmesh;
    public Map mymap;
    public Tile[] board = new Tile[512];
    public GameObject cursorcube;
    public int cursoridx;

    [Header("Materials")]
    public Material goodhighlight;
    public Material badhighlight;
    public int toPlaceDir;
    Manager()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Tried to make two managers!");
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 64; i++)
        {
            Instantiate(prefabs[(int)ToPlaceType.Platform], vecfromidx(i) - 5 * Vector3.up,Quaternion.identity);
        }
        mymap.init();

        ghostPrefabs = new Transform[prefabs.Length];
        for (int i=0; i< prefabs.Length; i++)
        {
            ghostPrefabs[i] = Instantiate(prefabs[i]);
            StripTransform(ghostPrefabs[i]);
            foreach (var r in ghostPrefabs[i].GetComponentsInChildren<Renderer>())
                r.material = goodhighlight;
        }
        
    }


    Quaternion rotFromInt(int dir)
    {
        //TO TEST: 0 means going up the stairs means going east, in the positive x direction.
        switch (dir)
        {
            case 0:
                return Quaternion.LookRotation(Vector3.right);
            case 1:
                return Quaternion.LookRotation(-Vector3.forward);
            case 2:
                return Quaternion.LookRotation(-Vector3.right);
            case 3:
                return Quaternion.LookRotation(Vector3.forward);
            default:
                return default;
        }
    }

    public Vector3 vecfromidx(int index)
    {
        return new Vector3(xfromidx(index), yfromidx(index), zfromidx(index)) * 5f;
    }

    public int xfromidx(int idx)
    {
        return idx & 7;
    }

    public int zfromidx(int idx)
    {
        return idx >> 3 & 7;
    }

    public int yfromidx(int idx)
    {
        return idx >> 6 & 7;
    }

    public int idxfromvec(Vector3 vec)
    {
        vec /= 5;
        return (Mathf.RoundToInt(vec.y) << 6) | (Mathf.RoundToInt(vec.z) << 3) | Mathf.RoundToInt(vec.x);
    }
    

    // Update is called once per frame
    /// <summary>
    /// Run the game.
    ///     - move the cursor around within the bounds.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            if (yfromidx(cursoridx) + 1 < 8)
            {
                cursoridx += 64;
            }
        }
        if (Input.GetKeyDown(KeyCode.Period))
        {
            if (yfromidx(cursoridx) - 1 >= 0)
            {
                cursoridx -= 64;
            }
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (zfromidx(cursoridx) + 1 < 8)
            {
                cursoridx += 8;
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (zfromidx(cursoridx) -1 >= 0)
            {
                cursoridx -= 8;
            }
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (xfromidx(cursoridx) + 1 < 8)
            {
                cursoridx += 1;
            }
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (xfromidx(cursoridx) - 1 >= 0) 
            {
                
                cursoridx -= 1;
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            toPlaceDir++;
            toPlaceDir %= 4;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (i == toPlaceNum)
                ghostPrefabs[i].gameObject.SetActive(true);
            else
                ghostPrefabs[i].gameObject.SetActive(false);
        }

        ghostPrefabs[toPlaceNum].position = vecfromidx(cursoridx);
        ghostPrefabs[toPlaceNum].rotation = rotFromInt(toPlaceDir);
        
        var isOkToPlace = IsOKToPlace((ToPlaceType) toPlaceNum, cursoridx);

        if (isOkToPlace)
        {
            foreach (var r in ghostPrefabs[toPlaceNum].GetComponentsInChildren<MeshRenderer>())
            {
                r.material = goodhighlight;
            }
        }
        else
        {
            foreach (var r in ghostPrefabs[toPlaceNum].GetComponentsInChildren<MeshRenderer>())
            {
                r.material = badhighlight;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isOkToPlace)
            {
                PlaceThing((ToPlaceType)toPlaceNum,toPlaceDir,cursoridx);
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            
            //which thing do you delete??
            //can you delete a thing that everything is leaning on?
            //it would be so cool 
        }
    }

    private void PlaceThing(ToPlaceType t, int dir, int idx)
    {
        var i = (int)t;
        var thing = Instantiate(prefabs[i], vecfromidx(idx), rotFromInt(dir));
        board[idx].things.Add(thing.gameObject);
        board[idx].thingsIHave |= 1 << i;
        board[idx].thingOrientations |= dir << (2 * i);
        mymap.board[idx]=board[idx];
    }

    public static int indexbelow (int index)
    {
        if (index >= 64)
        {
            return index - 64;
        }
        else
        {
            return -1;
        }
    }
    public static int indexabove(int index)
    {
        if (index < 512 - 64) 
        {
            return index + 64;
        }
        else
        {
            return -1;
        }
    }
    public static int typetoint (ToPlaceType t)
    {
        return 1 << (int)t;
    }
    public static bool hastype(ToPlaceType t,int bitvec)
    {
        return (typetoint(t) & bitvec) != 0;
    }
    public static int diroftype(ToPlaceType t, int dirvec)
    {
        return  dirvec >> (2 * (int)t) & 3;
    }

    public bool IsOKToPlace(ToPlaceType t, int idx)
    {
        var squarecontents = board[idx].thingsIHave;
        var vec = vecfromidx(idx);
        //rule 1
        if (hastype(t, squarecontents) || hastype(ToPlaceType.Stair,squarecontents))
        {
            return false;
        }
        //rule 2
        if (t == ToPlaceType.Stair)
        {
            if (squarecontents != 0)
            {
                return false;
            }
        }
        //rule 3
        var below = indexbelow(idx);
        if (below == -1 || hastype(ToPlaceType.Platform, board[below].thingsIHave))
        {
            return true;
        }
        //rule 4
        if (t == ToPlaceType.Stair && hastype(ToPlaceType.Stair, board[below].thingsIHave))
        { 
            if (diroftype(t, board[idx].thingOrientations) == diroftype(t, board[below].thingOrientations))
            {
                return true;
            }
        }
        //rule 5
        if (t == ToPlaceType.Platform && hastype(ToPlaceType.Stair, board[below].thingsIHave))
        {
            return true;
        }
        //rule 6
        return false;
    }
    
    /// <summary>
    /// Zack's vote for historically worst function
    /// </summary>
    /// <param name="g"></param>
    public void StripTransform(Transform g)
    {
        var joints = g.GetComponentsInChildren<Joint>();
        foreach (var r in joints)
        {
            DestroyImmediate(r);
        }

        var rbs = g.GetComponentsInChildren<Rigidbody>();
        foreach (var r in rbs)
        {
            DestroyImmediate(r);
        }

        var cs = g.GetComponentsInChildren<Collider>();
        foreach (var r in cs)
        {
            DestroyImmediate(r);
        }
        

        foreach (var r in g.GetComponentsInChildren<AudioSource>())
        {
            DestroyImmediate(r);
        }
        
    }
}