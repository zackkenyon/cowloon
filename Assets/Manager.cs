using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public enum TileType
{
    Empty,Platform,Stair
}

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
    public TileType type;
    public List<GameObject> things;
    public int thingsIHave;
    public int thingOrientations;
}


public class Manager : MonoBehaviour
{
    public static Manager Instance;
    public Transform[] prefabs;
    public Transform toPlace;
    public int toPlaceNum;
    public TMPro.TextMeshProUGUI Currenttextmesh;
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
    }


    Quaternion rotFromInt(int dir)
    {
        switch (dir)
        {
            case 0:
                return Quaternion.LookRotation(Vector3.forward);
            case 1:
                return Quaternion.LookRotation(Vector3.right);
            case 2:
                return Quaternion.LookRotation(-Vector3.forward);
            case 3:
                return Quaternion.LookRotation(-Vector3.right);
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
        cursorcube.transform.position = vecfromidx(cursoridx);
        var isOkToPlace = IsOKToPlace((ToPlaceType) toPlaceNum, cursoridx);

        if (isOkToPlace)
            cursorcube.GetComponentInChildren<MeshRenderer>().material = goodhighlight;
        else
            cursorcube.GetComponentInChildren<MeshRenderer>().material = badhighlight;

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
    }

    int indexbelow (int index)
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
    int indexabove(int index)
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
    public int typetoint (ToPlaceType t)
    {
        return 1 << (int)t;
    }
    public bool hastype(ToPlaceType t,int bitvec)
    {
        return (typetoint(t) & bitvec) != 0;
    }
    public int diroftype(ToPlaceType t, int dirvec)
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
}