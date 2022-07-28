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
public enum TileDir
{
    N,S,E,W
}
[Serializable]
public struct Tile
{
    public TileType type;
    public TileDir dir;
    public List<GameObject> things;
    public int thingsIHave;
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
        for (int i = 0; i < 63; i++)
        {
            UpdateTile(i, TileType.Platform);
        }

        UpdateTile(63, TileType.Stair);
        board[63].dir = TileDir.N;
    }
    void UpdateTile(int index, TileType t)
    {
        board[index].type = t;
        Transform thing;
        if (board[index].things == null)
        {
            board[index].things = new List<GameObject>();
        }

        switch (t)
        {
            case TileType.Empty:
                break;
            case TileType.Platform:
                thing = Instantiate(prefabs[0], vecfromidx(index), rotFromDir(board[index].dir));
                board[index].things.Add(thing.gameObject);
                board[index].thingsIHave |= 1 << (int) ToPlaceType.Platform;
                break;
            case TileType.Stair:
                thing = Instantiate(prefabs[1], vecfromidx(index), rotFromDir(board[index].dir));
                board[index].things.Add(thing.gameObject);
                board[index].thingsIHave |= 1 << (int) ToPlaceType.Stair;

                break;
            default:
                break;
        }
    }

    Quaternion rotFromDir(TileDir dir)
    {
        switch (dir)
        {
            case TileDir.N:
                return Quaternion.LookRotation(Vector3.forward);
            case TileDir.S:
                return Quaternion.LookRotation(-Vector3.forward);
            case TileDir.E:
                return Quaternion.LookRotation(Vector3.right);
            case TileDir.W:
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
                var thing = Instantiate(toPlace, cursorcube.transform.position, cursorcube.transform.rotation);
                board[cursoridx].things.Add(thing.gameObject);
                board[cursoridx].thingsIHave |= 1 << toPlaceNum;
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            
            //which thing do you delete??
            //can you delete a thing that everything is leaning on?
            //it would be so cool 
        }
    }

    public bool IsOKToPlace(ToPlaceType t, int idx)
    {
        var thingsihave = board[idx].thingsIHave;
        var vec = vecfromidx(idx);
        
        if ((thingsihave & (1 << (int) ToPlaceType.Stair)) != 0)
        {
            Debug.Log("can't place because there's already a stair there");
            return false;
        }

        if ((thingsihave & (1 << (int) ToPlaceType.Platform)) == 0)
        {
            if ((int) t > (int) ToPlaceType.Stair)
            {
                Debug.Log("can't place because there's no floor here");

                return false;
            }

            if (t == ToPlaceType.Platform)
            {
                //always allowed to put stuff on the bottom level
                if (vec.y == 0) return true;

                var undermevec = new Vector3(vec.x, vec.y - 5, vec.z);

                var undermeidx = idxfromvec(undermevec);
                var undermetile = board[undermeidx];
                Debug.Log($"my vec is {vec} and undermevec is {undermevec}");
                Debug.Log($"my idx is {idx} and under me idx is {undermeidx}" );
                //can't place a floor above a stair
                if ((undermetile.thingsIHave & (1 << (int) ToPlaceType.Stair)) != 0)
                {
                    Debug.Log("can't place because there's stair below this");
                    return false;
                }

                if ((undermetile.thingsIHave & (1 << (int) ToPlaceType.Platform)) == 0)
                {
                    Debug.Log("can't place because there's no floor below this");
                    return false;
                }
            }
            
        }



        return true;
    }
    
    
}