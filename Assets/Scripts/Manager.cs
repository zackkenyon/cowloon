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
    public FixedList512Bytes<int> things;
    public int thingsIHave;
    public int thingOrientations;
}


public class Manager : MonoBehaviour
{
    public static int idcounter=0;
    public Dictionary<int, GameObject> allthings;
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
    public int3 startnode;
    public int3 endnode;
    [Header("Materials")]
    public Material goodhighlight;
    public Material badhighlight;
    public int toPlaceDir;
    public AStar<int3> pathfinder;
    public NativeList<int3> shortestpath;
    public List<GameObject> pathcubes; 
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
    private void OnDestroy()
    {
        mymap.board.Dispose();
        shortestpath.Dispose();
    }
    // Start is called before the first frame update

    void Start()
    {
        for(int i = 0; i<100; i++)
        {
            pathcubes.Add(Instantiate(cursorcube));
            pathcubes[i].GetComponentInChildren<Renderer>().material = badhighlight;
            pathcubes[i].GetComponentInChildren<Renderer>().enabled = false;
        }
        startnode = new int3(-1, -1, -1);
        endnode = new int3(-1, -1, -1);
        pathfinder = new AStar<int3>(100, Allocator.Persistent);
        shortestpath = new NativeList<int3>(100, Allocator.Persistent);
        allthings = new Dictionary<int, GameObject>();
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
        if (Input.GetKeyDown(KeyCode.G))
        {
            startnode = Map.int3ofidx(cursoridx);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            endnode = Map.int3ofidx(cursoridx);
        }
        if (math.any(startnode != new int3(-1, -1, -1)) && math.any(endnode != new int3(-1, -1, -1)))
        {
            shortestpath.Length = 0;
            pathfinder.Clear();
            pathfinder.FindPath(mymap, startnode, endnode, shortestpath);
            Debug.Log(shortestpath.Length);
            foreach(var cube in pathcubes)
            {
                cube.GetComponentInChildren<Renderer>().enabled = false;
            }
            for (int i = 0; i < shortestpath.Length; i++)
            {
                pathcubes[i].transform.position = vecfromidx(Map.idxof(shortestpath[i]));
                pathcubes[i].GetComponentInChildren<Renderer>().enabled = true;
            }
        }
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
        allthings.Add(++idcounter,thing.gameObject);
        board[idx].things.Add(idcounter);
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