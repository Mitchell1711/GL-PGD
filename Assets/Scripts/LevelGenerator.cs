using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public enum TileType {
    Empty = 0,
    Player,
    Enemy,
    Wall,
    Door,
    Key,
    Dagger,
    End
}

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] tiles;
    public int rooms;
    public int minRoomSize;
    public int maxRoomSize;

    [Range(64, 128)]
    public int size;

    protected void Start()
    {
        //set random seed
        Random.InitState(System.DateTime.Now.Millisecond);

        TileType[,] grid = new TileType[size, size];
        Vector2Int[] nodes = new Vector2Int[rooms];

        FillBlock(grid, 0, 0, size, size, TileType.Wall);

        GenerateRooms(grid, nodes);
        GeneratePaths(grid, nodes);
        PopulateRooms(grid, nodes);

        //use 2d array (i.e. for using cellular automata)
        CreateTilesFromArray(grid);
    }

    private void GenerateRooms(TileType[,] grid, Vector2Int[] nodes)
    {
        for (int i = 0; i < rooms; i++)
        {
            bool isInValid = false;
            do
            {
                //generate random room location
                nodes[i] = new Vector2Int(Random.Range(maxRoomSize + 1, size - maxRoomSize), Random.Range(maxRoomSize + 1, size - maxRoomSize));

                //loop through each room and check if its not too close
                for (int j = 0; j < rooms; j++)
                {
                    isInValid = i != j && nodes[i].x > nodes[j].x - maxRoomSize * 2 && nodes[i].x < nodes[j].x + maxRoomSize * 2
                        && nodes[i].y > nodes[j].y - maxRoomSize * 2 && nodes[i].y < nodes[j].y + maxRoomSize * 2;

                    if (isInValid) break;
                }
            }
            while (isInValid);

            int roomSize = Random.Range(minRoomSize, maxRoomSize);

            //create room
            FillBlock(grid, nodes[i].x - roomSize, nodes[i].y - roomSize, roomSize * 2, roomSize * 2, TileType.Empty);
        }
    }

    private void GeneratePaths(TileType[,] grid, Vector2Int[] nodes)
    {
        //holds the room connections
        int[] connections = new int[rooms];

        for (int i = 0; i < rooms; i++)
        {
            float distance = int.MaxValue;

            //i do rooms - 1 here to make sure the ending room has only 1 connection
            for(int j = 0; j < rooms - 1; j++)
            {
                if (i != j && connections[j] == 0)
                {
                    //calculate distance between nodes
                    float distCheck = Vector2.Distance(nodes[i], nodes[j]);

                    //search for the nearest node to create a connection with
                    if (distCheck < distance)
                    {
                        distance = distCheck;
                        connections[i] = j;
                    }
                }
            }

            int fillWidth = nodes[connections[i]].x - nodes[i].x;
            int fillHeight = nodes[connections[i]].y - nodes[i].y;

            //create the paths
            FillBlock(grid, nodes[i].x, nodes[i].y, 1, fillHeight, TileType.Empty);
            FillBlock(grid, nodes[i].x, nodes[i].y + fillHeight, fillWidth, 1, TileType.Empty);

            //place door in last corridor
            if (i == rooms - 1)
            {
                if (fillHeight > maxRoomSize || fillHeight < -maxRoomSize) FillBlock(grid, nodes[i].x, (int)(nodes[i].y + maxRoomSize * Mathf.Sign(fillHeight)), 1, 1, TileType.Door);
                else FillBlock(grid, (int)(nodes[i].x + maxRoomSize * Mathf.Sign(fillWidth)), nodes[i].y + fillHeight, 1, 1, TileType.Door);
            }
        }
    }

    private void PopulateRooms(TileType[,] grid, Vector2Int[] nodes)
    {
        //place player in starting room
        FillBlock(grid, nodes[0].x, nodes[0].y, 1, 1, TileType.Player);

        //place key in another room
        FillBlock(grid, nodes[1].x, nodes[1].y, 1, 1, TileType.Key);

        //place daggers every 3 rooms
        for(int i = 2; i < rooms; i += 3) FillBlock(grid, nodes[i].x, nodes[i].y, 1, 1, TileType.Dagger);

        //place enemies every 3 rooms
        for(int i = 1; i < rooms; i += 3) FillBlock(grid, nodes[i].x + 1, nodes[i].y + 1, 1, 1, TileType.Enemy);

        //place exit in final room
        FillBlock(grid, nodes[rooms - 1].x, nodes[rooms - 1].y, 1, 1, TileType.End);
    }

    //fill part of array with tiles
    private void FillBlock(TileType[,] grid, int x, int y, int width, int height, TileType fillType) {
        //added support for negative fill width and height
        if (height >= 0)
        {
            for (int tileY = 0; tileY < height; tileY++)
            {
                FillX(grid, x, y, width, tileY, fillType);
            }
        }
        else
        {
            for (int tileY = 0; tileY > height; tileY--)
            {
                FillX(grid, x, y, width, tileY, fillType);
            }
        }
    }

    private void FillX(TileType[,] grid, int x, int y, int width, int tileY, TileType fillType)
    {
        if(width >= 0)
        {
            for (int tileX = 0; tileX < width; tileX++)
            {
                grid[tileY + y, tileX + x] = fillType;
            }
        }
        else
        {
            for (int tileX = 0; tileX > width; tileX--)
            {
                grid[tileY + y, tileX + x] = fillType;
            }
        }
    }

    //use array to create tiles
    private void CreateTilesFromArray(TileType[,] grid) {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y=0; y<height; y++) {
            for (int x=0; x<width; x++) {
                 TileType tile = grid[y, x];
                 if (tile != TileType.Empty) {
                     CreateTile(x, y, tile);
                 }
            }
        }
    }

    //create a single tile
    private GameObject CreateTile(int x, int y, TileType type) {
        int tileID = ((int)type) - 1;
        if (tileID >= 0 && tileID < tiles.Length)
        {
            GameObject tilePrefab = tiles[tileID];
            if (tilePrefab != null) {
                GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.SetParent(transform);
                return newTile;
            }

        } else {
            Debug.LogError("Invalid tile type selected");
        }

        return null;
    }
}