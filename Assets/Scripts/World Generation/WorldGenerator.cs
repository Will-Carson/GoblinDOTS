using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    // Parameters
    public Vector3Int WorldBounds = new Vector3Int(30, 10, 30);
    public int MaxNumberOfTiles = 2000;
    public float SizeMultiplier = .4f;
    public float RerollThreshold = .0001f;
    public float ResetThreshold = .8333f;
    public int CaveThreshold = 6;
    public int MaxRegionSize = 6;

    public bool RandomSeed = false;
    public int Seed = 1;
    public int StepTileNumbers = 100;
    public bool StepTiles = false;
    public bool ResetStep = false;
    public bool GenerateWorld = false;

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
    private int CurrentTiles;
    private GameObject Parent;
    private List<Vector3Int> RoomCoords = new List<Vector3Int>();
    private Vector3Int WorldCenter;
    private Vector3Int SnakeHead;
    private int CurrentTileStep;
    private int CurrentZoneId = 0;
    private Dictionary<int, Color> ZoneColors = new Dictionary<int, Color>();
    public List<List<int>> ZoneTileIds = new List<List<int>>();

    // Output
    public MazeTile[,,] Maze;

    private void Update()
    {
        if (GenerateWorld && !StepTiles)
        {
            GenerateWorld = false;
            Destroy(Parent);
            Parent = new GameObject();
            CurrentTiles = 0;
            RoomCoords.Clear();
            CurrentZoneId = 0;
            ZoneColors.Clear();
            if (RandomSeed)
            {
                var r = Random.Range(0, 99999);
                Random.InitState(r);
                Seed = r;
            }
            else
            {
                Random.InitState(Seed);
            }

            GenerateMaze();
            VisualizeMaze();
        }
        if (GenerateWorld && StepTiles)
        {
            GenerateWorld = false;
            Destroy(Parent);
            Parent = new GameObject();
            CurrentTiles = 0;
            RoomCoords.Clear();
            ZoneColors.Clear();
            Random.InitState(Seed);
            CurrentTileStep++;
            MaxNumberOfTiles = StepTileNumbers * CurrentTileStep;

            GenerateMaze();
            VisualizeMaze();
        }
        if (ResetStep)
        {
            ResetStep = false;
            CurrentTileStep = 0;
            CurrentZoneId = 0;
            ZoneColors.Clear();
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

        // Set tile coordinates
        for (int x = 0; x < WorldBounds.x; x++)
        {
            for (int y = 0; y < WorldBounds.y; y++)
            {
                for (int z = 0; z < WorldBounds.z; z++)
                {
                    if (Maze[x, y, z].isRoom)
                    {
                        Maze[x, y, z].coordinate = new Vector3Int(x, y, z);
                    }
                }
            }
        }

        // Set surface tiles
        for (int x = 0; x < WorldBounds.x; x++)
        {
            for (int z = 0; z < WorldBounds.z; z++)
            {
                for (int y = WorldBounds.y - 1; y >= 0; y--)
                {
                    if (Maze[x, y, z].isRoom && y > CaveThreshold)
                    {
                        Maze[x, y, z].isSurface = true;
                        break;
                    }
                }
            }
        }

        // Create zones
        var TempRoomCoords = RoomCoords;
        for (int i = 0; i < RoomCoords.Count; i++)
        {
            var r = Random.Range(0, TempRoomCoords.Count);
            var v = TempRoomCoords[r];
            TempRoomCoords.RemoveAt(r);
            var x = v.x;
            var y = v.y;
            var z = v.z;
            if (Maze[x, y, z].isRoom && Maze[x, y, z].zoneId == 0)
            {
                CurrentZoneId++;
                var tile = Maze[x, y, z];
                var roomsInZone = new List<MazeTile>();
                var neighbors = new Dictionary<Vector3Int, MazeTile>();
                var neighborsList = new List<MazeTile>();

                roomsInZone.Add(Maze[x, y, z]);

                while (roomsInZone.Count < MaxRegionSize)
                {
                    foreach (var room in roomsInZone)
                    {
                        neighborsList.AddRange(FindLikeNeighbors(tile.isSurface, room.coordinate));
                    }
                    foreach (var neighbor in neighborsList)
                    {
                        if (neighbors.ContainsKey(neighbor.coordinate)) continue;
                        neighbors.Add(neighbor.coordinate, neighbor);
                    }

                    var neighborArray = new Vector3Int[neighbors.Count];
                    neighbors.Keys.CopyTo(neighborArray, 0);
                    if (neighborArray.Length == 0) break;
                    var c = neighborArray[Random.Range(0, neighborArray.Length)];
                    var newTileInZone = Maze[c.x, c.y, c.z];
                    roomsInZone.Add(newTileInZone);
                }

                for (int j = 0; j < roomsInZone.Count; j++)
                {
                    var c = roomsInZone[j].coordinate;
                    Maze[c.x, c.y, c.z].zoneId = CurrentZoneId;
                }
            }
        }
    }

    private List<MazeTile> FindLikeNeighbors(bool isSurface, Vector3Int coordinate)
    {
        var validNeighbors = new List<MazeTile>();
        var validTile = new MazeTile();
        if (ValidNeighborTile(Direction.North, coordinate, isSurface, out validTile))
        {
            validNeighbors.Add(validTile);
        }
        if (ValidNeighborTile(Direction.South, coordinate, isSurface, out validTile))
        {
            validNeighbors.Add(validTile);
        }
        if (ValidNeighborTile(Direction.East, coordinate, isSurface, out validTile))
        {
            validNeighbors.Add(validTile);
        }
        if (ValidNeighborTile(Direction.West, coordinate, isSurface, out validTile))
        {
            validNeighbors.Add(validTile);
        }
        if (ValidNeighborTile(Direction.Up, coordinate, isSurface, out validTile))
        {
            validNeighbors.Add(validTile);
        }
        if (ValidNeighborTile(Direction.Down, coordinate, isSurface, out validTile))
        {
            validNeighbors.Add(validTile);
        }
        return validNeighbors;
    }

    private bool ValidNeighborTile(Direction d, Vector3Int coordinate, bool isSurface, out MazeTile tile)
    {
        tile = new MazeTile();
        var direction = coordinate + Directions[d];
        var oldTile = Maze[coordinate.x, coordinate.y, coordinate.z];
        if (IsInBounds(WorldBounds, direction))
        {
            tile = Maze[direction.x, direction.y, direction.z];
            if (tile.isSurface == isSurface &&
                tile.isRoom &&
                tile.zoneId == 0 &&
                oldTile.connections.Contains(d))
            {
                return true;
            }
        }
        return false;
    }

    private void IterateMaze()
    {
        var validDirections = FindValidDirections(WorldBounds, SnakeHead);
        if (validDirections.Count < 6 && Random.Range(0f, 1f) > ResetThreshold)
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
        if (Maze[newSnakeHead.x, newSnakeHead.y, newSnakeHead.z].connections == null)
        {
            Maze[newSnakeHead.x, newSnakeHead.y, newSnakeHead.z].connections = new HashSet<Direction>();
        }

        MakeNewRoom(SnakeHead, d);
        SnakeHead = newSnakeHead;
    }

    private void MakeNewRoom(Vector3Int oldSnakeHead, Direction direction)
    {
        var x = oldSnakeHead.x;
        var y = oldSnakeHead.y;
        var z = oldSnakeHead.z;
        Maze[x, y, z].connections.Add(direction);

        var newSnakeHead = oldSnakeHead + Directions[direction];
        x = newSnakeHead.x;
        y = newSnakeHead.y;
        z = newSnakeHead.z;
        if (Maze[x, y, z].isRoom == false) CurrentTiles++;
        Maze[x, y, z].isRoom = true;
        Maze[x, y, z].connections.Add(ReverseDirection(direction));
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
                    var tile = Maze[x, y, z];
                    if (tile.isRoom)
                    {
                        var color = new Color();
                        if (tile.isSurface)
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
                        
                        foreach (var mazeTile in tile.connections)
                        {
                            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            s.transform.position = new Vector3(x, y, z) + ((Vector3)Directions[mazeTile] * SizeMultiplier);
                            s.transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, SizeMultiplier);
                            s.transform.parent = Parent.transform;
                            s.GetComponent<Renderer>().material.color = color;
                        }

                        if (!ZoneColors.ContainsKey(tile.zoneId))
                        {
                            var newColor = new Color(
                                Random.Range(0f, 1f),
                                Random.Range(0f, 1f),
                                Random.Range(0f, 1f)
                            );
                            ZoneColors.Add(tile.zoneId, newColor);
                        }

                        var zoneColor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        zoneColor.transform.position = new Vector3(x, y + (.5f * SizeMultiplier), z);
                        zoneColor.transform.localScale = new Vector3(SizeMultiplier, SizeMultiplier, SizeMultiplier) * .5f;
                        zoneColor.transform.parent = Parent.transform;
                        zoneColor.GetComponent<Renderer>().material.color = ZoneColors[tile.zoneId];
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
    public int zoneId;
    public Vector3Int coordinate;
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