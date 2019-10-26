using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

/*
 * In this game only two types of colliders are used - one for asteroid and one for bullet.
 * Player uses same collider as asteroid for simplicity.
 * Both colliders are circles, so circle-circle collision is only kind of collision this class
 * can detect.
 * 
 * Colliders are grouped into slightly overlapping cells and only collisions between colliders
 * inside same cells are checked. Cells at the edges of grid are infinite at one (two at the
 * corners) side, so all colliders are always simulated.
 * Every some time grid should be resized to better match simulation size, as it will be
 * naturally growing and more and more asteroids will be in cells at the edges which will
 * decrease performance over time. This is not yet implemented.
 * 
 * 
 * Cells are numbered this way:
 * 
 * 6    7   8
 * 3    4   5
 * 0    1   2
 * 
 * or
 * 
 * (0,2)    (1,2)   (2,2)
 * (0,1)    (1,1)   (2,1)
 * (0,0)    (1,0)   (2,0)
 * 
 */

public sealed class CirclePhysicsManager : MonoBehaviour
{
    public static CirclePhysicsManager singleton;

    public float timeSinceLevelLoad { get; private set; }

    //moved from Asteroid class
    public const float gridUnit = 3f;          //width an height of grid tile
    public const int gridSize = 160;
    public const int halfGridSize = gridSize / 2;
    public const bool isGridSizeEven = ((gridSize % 2) != 1);
    public const float maxSpeed = 2f;          //maximum speed will be this vlaue squared
    public const float minSpeed = 0.1f;        //minimum speed will be this vlaue squared
    public const float respawnTime = 1f;       //time after which destroyed asteroid will respawn
    //end

    const float asteroidRadius = 0.4f;
    const float bulletRadius = 0.15f;
    const int cellGridSize = 64;    //must be even!
    const float cellUnitSize = 9;   //size of single cell (actuall cells will be bigger by cellOverlap)
    const float cellOverlap = 1f;   //how much cells overlap

    //squared minimal collision distances between colliders
    const float asteroidAsteroid = (asteroidRadius + asteroidRadius) * (asteroidRadius + asteroidRadius);
    const float asteroidBullet = (asteroidRadius + bulletRadius) * (asteroidRadius + bulletRadius);

    public const int numberOfAsteroids = gridSize * gridSize;
    const int numberOfObjects = gridSize * gridSize + ShipControl.maxBullets + 1;//+1 is player
    const int cellNumber = cellGridSize * cellGridSize;  //total number of cells
    const int cellNumberMinOne = cellNumber - 1;  //total number of cells minus one
    const float gridUnitSize = cellGridSize * cellUnitSize;
    const float halfGridUnitSize = gridUnitSize / 2f;

    const int asteroidsCheckedForRespawnPerFrame = 5120;

    public bool simulate = true;    //set to false to stop collision detection
    public Transform playerTransform { get; private set; }

    CircleCollider[] colliders = new CircleCollider[numberOfObjects];           //list of all colliders
    CircleCollider[] asteroids = new CircleCollider[numberOfAsteroids];         //list of all asteroids
    List<CircleCollider>[] cells = new List<CircleCollider>[cellNumber];        //array of lists containing colliders which are currently in given cells
    List<CircleCollider>[] startCells = new List<CircleCollider>[cellNumber];   //used to skip reassigning cells at game restart

    //use floats to store time because coroutines allocate memory whenever they are called
    Dictionary<CircleCollider, float> asteroidRespawnTimers = new Dictionary<CircleCollider, float>(numberOfAsteroids);

    //cells are defined by their maximum and minimum x and y coordinates
    Vector3[] cellMaximums = new Vector3[cellNumber];
    Vector3[] cellMinimums = new Vector3[cellNumber];

    //keep track of cells in which player currently is to assign bullets to those cells after they are teleported back to player
    List<int> playerCells = new List<int>(5);

    //Vector3 distanceVector;                 //used in collision detection
    bool isAssigned;                        //used to exit cell assigning loop early
    List<int> toRemove = new List<int>(5);  //used to remove colliders that left cell from this cell's collider list
    bool assignedenything;                  //used to check if collider just moved to one of nearby cells or teleported
    Vector3 asteroidRespawnVector = new Vector3(1f, 3f);//used to respawn asteroids inside cells
    int randomCellId;                       //used to randomize asteroid's respawn position
    Vector3 randomRespawnVector = new Vector3();//used to respawn asteroids
    public bool initialized { get; private set; } = false;//used to call LateStart, which has to be executed after all other object's Start methods
    int lastAddedCollider = 0;              //id of last collider added to colliders array

    //cells can be inside grid or at it's edges
    enum CellPosition
    {
        inside,
        left,
        right,
        up,
        down,
        leftUp,
        rightUp,
        leftDown,
        rightDown
    }

    void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else if (singleton != this)
        {
            Destroy(gameObject);
        }

        for (int i = 0; i < cellNumber; i++)
        {
            cells[i] = new List<CircleCollider>(9);
        }

        //calculate cell borders
        for (int y = 0; y < cellGridSize; y++)
        {
            for (int x = 0; x < cellGridSize; x++)
            {
                cellMaximums[(y * cellGridSize) + x] = new Vector3(halfGridUnitSize - (gridUnitSize - (x * cellUnitSize)), halfGridUnitSize - (gridUnitSize - (y * cellUnitSize)));
                cellMinimums[(y * cellGridSize) + x] = new Vector3(halfGridUnitSize - ((gridUnitSize - ((x - 1) * (cellUnitSize))) + cellOverlap), halfGridUnitSize - ((gridUnitSize - ((y - 1) * (cellUnitSize))) + cellOverlap));
            }
        }

    }

    //called by CircleCollider in it's constructor
    public void AddCircleCollider(CircleCollider collider)
    {
        colliders[lastAddedCollider] = collider;
        lastAddedCollider++;
    }


    void LateStart()
    {
        if (colliders[numberOfObjects - 1] == null) { return; }    //wait until all objects add themselfes to this array

        int asteroidId = 0;
        //assign playerTransform
        foreach (CircleCollider c in colliders)
        {
            if (c.isPlayer)
            {
                playerTransform = c.transform;
                AsteroidManager.singleton.playerTransform = c.transform;
            }
            else if (c.isAsteroid)
            {
                asteroids[asteroidId] = c;
                asteroidRespawnTimers.Add(c, 0f);
                asteroidId++;
            }
        }

        //assign all colliders to proper cells
        for (int i = 0; i < numberOfObjects; i++)
        {
            AssignCells(colliders[i]);
        }

        //make copies of all lists in cells array to use them when game resets
        for (int i = 0; i < cellNumber; i++)
        {
            startCells[i] = new List<CircleCollider>();
            for (int k = 0; k < cells[i].Count; k++)
            {
                startCells[i].Add(cells[i][k]);
            }
        }
        randomCellId = Random.Range(0, cellNumber - 1);
        initialized = true;
    }

    int asteroidsCheckedForRespawn = asteroidsCheckedForRespawnPerFrame;
    void Update()
    {
        if (!initialized)
        {
            LateStart();
            return;
        }

        #region respawn

        UnityEngine.Profiling.Profiler.BeginSample("respawn");
        //check if any asteroids should be respawned
        if (asteroidsCheckedForRespawn >= numberOfAsteroids) { asteroidsCheckedForRespawn = asteroidsCheckedForRespawnPerFrame; }
        for (int i = 0; i < asteroidsCheckedForRespawn; i++)
        {
            if (asteroidRespawnTimers[asteroids[i]] == 0f) { continue; }
            if (Time.timeSinceLevelLoad - asteroidRespawnTimers[asteroids[i]] > respawnTime)
            {
                RespawnAsteroidNow(asteroids[i]);
            }
        }
        asteroidsCheckedForRespawn += asteroidsCheckedForRespawnPerFrame;
        UnityEngine.Profiling.Profiler.EndSample();

        #endregion

        #region update cells
        UnityEngine.Profiling.Profiler.BeginSample("cells");

        playerCells.Clear();
        //check which colliders left their cells, remove them from those cell's lists and assign them to their current cells
        for (int i = 0; i < cellNumber; i++)
        {
            if (cells[i].Count > 0)
            {
                toRemove.Clear();
                for (int j = 0; j < cells[i].Count; j++)
                {
                    if (cells[i][j].isPlayer)
                    {
                        playerCells.Add(i);
                    }
                    if (!IsInCell(i, cells[i][j]))
                    {
                        AssignCells(cells[i][j], i);
                        toRemove.Add(j);
                    }
                }
                if (toRemove.Count > 0)
                {
                    for (int j = toRemove.Count - 1; j >= 0; j--)
                    {
                        cells[i].RemoveAt(toRemove[j]);
                    }
                }
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();

        #endregion

        #region check for collisions
        UnityEngine.Profiling.Profiler.BeginSample("collide");

        timeSinceLevelLoad = Time.timeSinceLevelLoad;

        if (simulate)
        {
            // TO DO: Benchmark single (current implementation) vs multiple cells per Paralell.For iteration

            //for (int cellId = 0; cellId < cellNumber; cellId++)
            Parallel.For(0, cellNumberMinOne, (int cellId) =>
            {
                if (cells[cellId].Count < 2) { return; }

                Vector3 distanceVector;
                
                for (int i = 0; i < cells[cellId].Count; i++)
                {
                    if (!cells[cellId][i].detect) { continue; }
                    if (cells[cellId][i].isAsteroid)
                    {
                        for (int j = i + 1; j < cells[cellId].Count; j++)
                        {
                            if (!cells[cellId][j].detect) { continue; }

                            distanceVector = cells[cellId][i].position - cells[cellId][j].position;
                            if (cells[cellId][j].isAsteroid)
                            {
                                if (Vector3.Dot(distanceVector, distanceVector) <= asteroidAsteroid)
                                {
                                    cells[cellId][i].Collide();
                                    cells[cellId][j].Collide();
                                }
                            }
                            else
                            {
                                if (cells[cellId][i].isPlayer) { continue; }    //player shouldn't collide with their own bullets
                                if (Vector3.Dot(distanceVector, distanceVector) <= asteroidBullet)
                                {
                                    cells[cellId][i].Collide();
                                    cells[cellId][j].Collide();
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = i + 1; j < cells[cellId].Count; j++)
                        {
                            if (!cells[cellId][j].detect) { continue; }

                            if (cells[cellId][j].isAsteroid && !cells[cellId][j].isPlayer)
                            {
                                distanceVector = cells[cellId][i].position - cells[cellId][j].position;
                                if (Vector3.Dot(distanceVector, distanceVector) <= asteroidBullet)
                                {
                                    cells[cellId][i].Collide();
                                    cells[cellId][j].Collide();
                                }
                            }
                            else
                            {
                                continue;   //bullets don't collide with eachother nor with player
                            }
                        }
                    }
                }
            }
            );
        }

        UnityEngine.Profiling.Profiler.EndSample();

        #endregion
    }

    //check if point is in box defined by min and max, but remember that cells at the edges are infinite
    bool IsInCell(Vector3 min, Vector3 max, Vector3 point, CellPosition cellPosition)
    {
        switch (cellPosition)
        {
            case CellPosition.inside:
                if (point.x >= min.x && point.y >= min.y && point.x <= max.x && point.y <= max.y)
                {
                    return true;
                }
                break;
            case CellPosition.left:
                if (point.y >= min.y && point.x <= max.x && point.y <= max.y)
                {
                    return true;
                }
                break;
            case CellPosition.right:
                if (point.x >= min.x && point.y >= min.y && point.y <= max.y)
                {
                    return true;
                }
                break;
            case CellPosition.up:
                if (point.x >= min.x && point.y >= min.y && point.x <= max.x)
                {
                    return true;
                }
                break;
            case CellPosition.down:
                if (point.x >= min.x && point.x <= max.x && point.y <= max.y)
                {
                    return true;
                }
                break;
            case CellPosition.leftUp:
                if (point.y >= min.y && point.x <= max.x)
                {
                    return true;
                }
                break;
            case CellPosition.rightUp:
                if (point.x >= min.x && point.y >= min.y)
                {
                    return true;
                }
                break;
            case CellPosition.leftDown:
                if (point.x <= max.x && point.y <= max.y)
                {
                    return true;
                }
                break;
            case CellPosition.rightDown:
                if (point.x >= min.x && point.y <= max.y)
                {
                    return true;
                }
                break;
            default:
                Debug.LogError("Unexpected cell position, can't check if collider is inside");
                break;
        }


        return false;
    }

    //checki if collider is in cell (x and y are grid coordinates)
    bool IsInCell(int x, int y, CircleCollider collider)
    {
        if (x < 0 || x >= cellGridSize || y < 0 || y >= cellGridSize)
        {
            //return false when cell doesn't exist
            return false;
        }

        //check if cell is at the edge of grid
        CellPosition cellPosition;
        if (x != 0 && x != cellGridSize - 1 && y != 0 && y != cellGridSize - 1)
        {
            cellPosition = CellPosition.inside;
        }
        else if (y == 0)
        {
            if (x != 0 && x != cellGridSize - 1)
            {
                cellPosition = CellPosition.down;
            }
            else if (x == 0)
            {
                cellPosition = CellPosition.leftDown;
            }
            else
            {
                cellPosition = CellPosition.rightDown;
            }
        }
        else if (x == 0)
        {
            if (y != cellGridSize - 1)
            {
                cellPosition = CellPosition.left;
            }
            else
            {
                cellPosition = CellPosition.leftUp;
            }
        }
        else if (y == cellGridSize - 1)
        {
            if (x != cellGridSize - 1)
            {
                cellPosition = CellPosition.up;
            }
            else
            {
                cellPosition = CellPosition.rightUp;
            }
        }
        else
        {
            cellPosition = CellPosition.down;
        }

        return IsInCell(cellMinimums[(y * cellGridSize) + x], cellMaximums[(y * cellGridSize) + x], collider.position, cellPosition);
    }

    //check if collider is in cell (id is cell's id in cells array)
    bool IsInCell(int id, CircleCollider collider)
    {
        return IsInCell(id % cellGridSize, id / cellGridSize, collider);
    }

    //iterate through all cells and add collider to those in which it currently is
    void AssignCells(CircleCollider collider, bool checkNearby = true)
    {
        isAssigned = false;
        for (int y = 0; y < cellGridSize && !isAssigned; y++)
        {
            for (int x = 0; x < cellGridSize; x++)
            {
                if (IsInCell(x, y, collider))
                {
                    cells[(y * cellGridSize) + x].Add(collider);
                    if (checkNearby)
                    {
                        AssignCells(collider, (y * cellGridSize) + x);  //if collider is in cell it can't be in any other cells but ones aroud that cell
                    }
                    isAssigned = true;
                    break;
                }
            }
        }
    }

    //check cells around previousCell and add collider to those in which it currently is
    void AssignCells(CircleCollider collider, int previousCell)
    {
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (x == 0 && y == 0) { continue; }
                if (IsInCell(previousCell + ((y * cellGridSize) + x), collider))
                {

                    if (!cells[(previousCell + ((y * cellGridSize) + x))].Contains(collider))
                    {
                        cells[(previousCell + ((y * cellGridSize) + x))].Add(collider);
                    }
                }
            }
        }
    }

    public void Restart()
    {
        StopAllCoroutines();
        //reset all colliders' cells
        for (int i = 0; i < cellNumber; i++)
        {
            cells[i].Clear();
            for (int k = 0; k < startCells[i].Count; k++)
            {
                cells[i].Add(startCells[i][k]);
            }
        }
        //reset asteroids' positions
        AsteroidManager.singleton.Restart();

        //reset respawn timers
        foreach (CircleCollider c in colliders)
        {
            if (!c.isPlayer && c.isAsteroid)
            {
                asteroidRespawnTimers[c] = 0f;
            }
        }
        simulate = true;
    }

    bool isTooClose;
    public void RespawnAsteroid(CircleCollider collider)
    {
        asteroidRespawnTimers[collider] = timeSinceLevelLoad;
    }

    void RespawnAsteroidNow(CircleCollider collider)
    {
        do
        {
            isTooClose = false;
            randomCellId = (randomCellId == cellNumber - 1) ? 0 : randomCellId + 1;
            if (Mathf.Abs(cellMinimums[randomCellId].x - playerTransform.position.x) < 10f && Mathf.Abs(cellMinimums[randomCellId].y - playerTransform.position.y) < 10f)
            {
                isTooClose = true;
            }
        } while (isTooClose);
        randomRespawnVector.x = Random.value * 5f;
        collider.position = cellMinimums[randomCellId] + asteroidRespawnVector + randomRespawnVector;
        cells[randomCellId].Add(collider);
        collider.detect = true;
        asteroidRespawnTimers[collider] = 0f;
    }

    public void AssignSameCellAsPlayer(CircleCollider collider)
    {
        for (int i = 0; i < playerCells.Count; i++)
        {
            if (!cells[playerCells[i]].Contains(collider))
            {
                cells[playerCells[i]].Add(collider);
            }
        }
    }

}