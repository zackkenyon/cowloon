using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Empty,Platform,Stair
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
    public GameObject thing;
}

public class Manager : MonoBehaviour
{
    public static Manager Instance;
    public Transform cowprefab;
    public Transform stairprefab;
    public Transform floorprefab;
    public Transform wallprefab;
    public Transform[] prefabs;
    public Transform toPlace;

    public Tile[] board = new Tile[512];
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
        if (board[index].thing != null)
        {
            Destroy(board[index].thing);
            board[index].thing = null;
        }

        switch (t)
        {
            case TileType.Empty:
                break;
            case TileType.Platform:
                thing = Instantiate(floorprefab, vecfromidx(index), rotFromDir(board[index].dir));
                board[index].thing = thing.gameObject;
                break;
            case TileType.Stair:
                thing = Instantiate(stairprefab, vecfromidx(index), rotFromDir(board[index].dir));
                board[index].thing = thing.gameObject;
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

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha0))
            toPlace = cowprefab;
        else if (Input.GetKey(KeyCode.Alpha1))
            toPlace = floorprefab;
        else if (Input.GetKey(KeyCode.Alpha2))
            toPlace = stairprefab;
        else if (Input.GetKey(KeyCode.Alpha3))
            toPlace = wallprefab;

        var didhit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitinfo);

        if (didhit)
        {
            
        }


    }
}