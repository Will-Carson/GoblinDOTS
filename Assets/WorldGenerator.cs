using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    // Parameters
    public Vector3Int WorldBounds = new Vector3Int(30, 10, 30);
    public int MaxNumberOfTiles = 2000;
    public float SizeMultiplier = .4f;
    public float RerollThreshold = .01f;
    public bool generateWorld = false;
    public int CaveThreshold = 5;

    // Helpers
    private Dictionary<Direction, Vector3Int> Directions = new Dictionary<Direction, Vector3Int>()
    {
        { Direction.North, new Vector3Int(1, 0, 0) },
        { Direction.South, new Vector3Int(-1, 0, 0) },
        { Direction.East, new Vector3Int(0, 0, 1) },
        { Direction.West, new Vector3Int(0, 0, -1) },
        { Direction.Up, new Vector3Int(0, 1, 0) },
        { Direction.Down, new Vector3Int(0, -1, 0) }
    };
    private MazeTile[,,] Maze;
    private int CurrentTiles;
    private GameObject Parent;
    private List<Vector3Int> RoomCoords;

    // Output
    private Vector3Int WorldCenter;
    private Vector3Int SnakeHead;

    private void Update()
    {
        if (generateWorld)
        {
            generateWorld = false;
            Destroy(Parent);
            Parent = new GameObject();
            CurrentTiles = 0;
            RoomCoords = new List<Vector3Int>();

            GenerateMaze();
            VisualizeMaze();
        }
    }

    #region GenerateMaze
    private void GenerateMaze()
    {
        Maze = new MazeTile[WorldBounds.x, WorldBounds.y, WorldBounds.z];
        WorldCenter = Find3DArrayCenterVector(WorldBounds.x, WorldBounds.y, WorldBounds.z);
        Maze[WorldCenter.x, WorldCenter.y, WorldCenter.z].isRoom = true;
        Maze[WorldCenter.x, WorldCenter.y, WorldCenter.z].connections = new HashSet<Direction>();
        RoomCoords.Add(WorldCenter);
        SnakeHead = WorldCenter;
        while (CurrentTiles <= MaxNumberOfTiles) IterateMaze();

        for (int x = 0; x < WorldBounds.x; x++)
        {
            for (int z = 0; z < WorldBounds.z; z++)
            {
                for (int y = WorldBounds.y - 1; y >= 0; y--)
                {
                    if (Maze[x, y, z].isRoom && y > CaveThreshold) // TODO Magic number
                    {
                        Maze[x, y, z].isSurface = true;
                        break;
                    }
                }
            }
        }
    }

    private void IterateMaze()
    {
        var validDirections = FindValidDirections(WorldBounds, SnakeHead);
        if (validDirections.Count < 6)
        {
            SnakeHead = RoomCoords[Random.Range(0, RoomCoords.Count)];
            return;
        }

        var d = validDirections[Random.Range(0, validDirections.Count)];
        if (d == Direction.Up || d == Direction.Down)
        {
            var r = Random.Range(0f, 1f);
            if (r > RerollThreshold)
            {
                d = validDirections[Random.Range(0, validDirections.Count)];
            }
        }
        var newSnakeHead = SnakeHead + Directions[d];
        Maze[newSnakeHead.x, newSnakeHead.y, newSnakeHead.z].connections = new HashSet<Direction>();

        MakeNewRoom(SnakeHead, d);
        SnakeHead = newSnakeHead;
    }

    private void MakeNewRoom(Vector3Int oldSnakeHead, Direction snakeHead)
    {
        var x = oldSnakeHead.x;
        var y = oldSnakeHead.y;
        var z = oldSnakeHead.z;
        Maze[x, y, z].connections.Add(snakeHead);

        var newSnakeHead = oldSnakeHead + Directions[snakeHead];
        x = newSnakeHead.x;
        y = newSnakeHead.y;
        z = newSnakeHead.z;
        if (Maze[x, y, z].isRoom == false) CurrentTiles++;
        Maze[x, y, z].isRoom = true;
        Maze[x, y, z].connections.Add(ReverseDirection(snakeHead));
        RoomCoords.Add(newSnakeHead);
    }

    private Direction ReverseDirection(Direction snakeHead)
    {
        if (snakeHead == Direction.North) return Direction.South;
        if (snakeHead == Direction.South) return Direction.North;
        if (snakeHead == Direction.East) return Direction.West;
        if (snakeHead == Direction.West) return Direction.East;
        if (snakeHead == Direction.Up) return Direction.Down;
        if (snakeHead == Direction.Down) return Direction.Up;
        return Direction.North;
    }

    private Vector3Int Find3DArrayCenterVector(int x, int y, int z)
    {
        return new Vector3Int(x / 2, y / 2, z / 2);
    }

    private List<Direction> FindValidDirections(Vector3Int bounds, Vector3Int location)
    {
        var validDirections = new List<Direction>();
        if (IsInBounds(bounds, location + Directions[Direction.North])) validDirections.Add(Direction.North);
        if (IsInBounds(bounds, location + Directions[Direction.South])) validDirections.Add(Direction.South);
        if (IsInBounds(bounds, location + Directions[Direction.East])) validDirections.Add(Direction.East);
        if (IsInBounds(bounds, location + Directions[Direction.West])) validDirections.Add(Direction.West);
        if (IsInBounds(bounds, location + Directions[Direction.Up])) validDirections.Add(Direction.Up);
        if (IsInBounds(bounds, location + Directions[Direction.Down])) validDirections.Add(Direction.Down);
        return validDirections;
    }

    private bool IsInBounds(Vector3Int bounds, Vector3Int location)
    {
        var test = true;
        if (location.x >= bounds.x) test = false;
        if (location.y >= bounds.y) test = false;
        if (location.z >= bounds.z) test = false;
        if (location.x < 0) test = false;
        if (location.y < 0) test = false;
        if (location.z < 0) test = false;
        return test;
    }
    #endregion

    private void VisualizeMaze()
    {
        for (int x = 0; x < WorldBounds.x; x++)
        {
            for (int y = 0; y < WorldBounds.y; y++)
            {
                for (int z = 0; z < WorldBounds.z; z++)
                {
                    if (Maze[x,y,z].isRoom)
                    {
                        var color = new Color();
                        if (Maze[x, y, z].isSurface)
                        {
                            color = Color.green;
                        }
                        else
                        {
                            color = Color.gray;
                        }

                        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c.transform.position = new Vector3(x, y, z);
                        c.transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, SizeMultiplier);
                        c.transform.parent = Parent.transform;
                        c.GetComponent<Renderer>().material.color = color;
                        
                        foreach (var mazeTile in Maze[x, y, z].connections)
                        {
                            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            s.transform.position = new Vector3(x, y, z) + ((Vector3)Directions[mazeTile] * SizeMultiplier);
                            s.transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, SizeMultiplier);
                            s.transform.parent = Parent.transform;
                            s.GetComponent<Renderer>().material.color = color;
                        }
                    }
                }
            }
        }
    }
}

public struct MazeTile
{
    public bool isRoom;
    public bool isSurface;
    public HashSet<Direction> connections;
}

public enum Direction
{
    North,
    South,
    East,
    West,
    Up,
    Down
}