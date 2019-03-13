using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AsteroidManager : MonoBehaviour
{

    public static AsteroidManager singleton = null;

    const float asteroidTextureSize = 100f;
    const float halfAsteroidTextureSize = asteroidTextureSize / 2f;
    const float asteroidRenderDistance = 10f;

    //constants from CirclePhysicsManager
    const float gridUnit = CirclePhysicsManager.gridUnit;          //width an height of grid tile
    const int gridSize = CirclePhysicsManager.gridSize;
    const int halfGridSize = CirclePhysicsManager.halfGridSize;
    const bool isGridSizeEven = CirclePhysicsManager.isGridSizeEven;
    const float maxSpeed = CirclePhysicsManager.maxSpeed;          //maximum speed will be this vlaue squared
    const float minSpeed = CirclePhysicsManager.minSpeed;        //minimum speed will be this vlaue squared
    const float respawnTime = CirclePhysicsManager.respawnTime;       //time after which destroyed asteroid will respawn

    const int numberOfAsteroids = CirclePhysicsManager.numberOfAsteroids;
    //end

    //drawing esteroids
    [HideInInspector]
    public Transform playerTransform;
#pragma warning disable CS0649
    [SerializeField]
    [Tooltip("Texture used by asteroids")]
    Texture asteroidTexture;
    [SerializeField] Vector3[] asteroidVelocities;  //velocities of all asteroids
#pragma warning restore CS0649
    bool positionsChanged;                          //don't calculate asteroids' GUI positions if it's not necessary
    List<Rect> lastDrawn = new List<Rect>();        //cache results of OnGUI because positions change only in Update
    Rect rect;                                      //used in OnGUI
    new Camera camera;
    //asteroids not between min and max coordinates are not rendered
    Vector3 cameraMax;
    Vector3 cameraMin;

    //movement
    CircleCollider[] asteroids = new CircleCollider[numberOfAsteroids];         //list of all colliders representing asteroids
    Vector3[] startAsteroidsPositions = new Vector3[numberOfAsteroids];         //used to restart positions of asteroids when game is restarted

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

        camera = Camera.main;
    }

    void Start()
    {
        Vector3 position;
        Vector3 offset = new Vector3(gridUnit / 2f, gridUnit / 2f);
        int currentAsteroidNumber = 0;
        //spawn asteroids
        for (int i = gridSize; i > 0; i--)
        {
            for (int j = gridSize; j > 0; j--)
            {
                position = new Vector3(((halfGridSize - (gridSize - i)) * gridUnit), ((halfGridSize - (gridSize - j)) * gridUnit));
                //add offset so player can start at (0,0)
                position += offset;
                asteroids[currentAsteroidNumber] = new CircleCollider(position);
                startAsteroidsPositions[currentAsteroidNumber] = position;
                currentAsteroidNumber++;
            }
        }
    }

    void OnValidate()
    {
        //validate velocities array
        if (asteroidVelocities == null || asteroidVelocities.Length != numberOfAsteroids)
        {
            asteroidVelocities = new Vector3[numberOfAsteroids];
            for (int i = 0; i < numberOfAsteroids; i++)
            {
                asteroidVelocities[i] = Quaternion.Euler(0, 0, Random.Range(0, 360)) * new Vector3(Random.Range(minSpeed, maxSpeed), Random.Range(minSpeed, maxSpeed));
            }
            Debug.LogWarning("asteroidVelocities.Length didn't match numberOfAsteroids so whole array was discarded and automatically generated");
        }
    }

    void OnGUI()
    {
        //OnGUI can be called many times per frame, so cache results and recalculate only after Update
        if (!positionsChanged)
        {
            for (int i = 0; i < lastDrawn.Count; i++)
            {
                GUI.DrawTexture(lastDrawn[i], asteroidTexture);
            }
        }
        else
        {
            lastDrawn.Clear();
            for (int i = 0; i < numberOfAsteroids; i++)
            {
                if (!asteroids[i].detect) { continue; }
                if (asteroids[i].position.y > cameraMax.y || asteroids[i].position.y < cameraMin.y || asteroids[i].position.x < cameraMin.x || asteroids[i].position.x > cameraMax.x)
                {
                    continue;
                }
                Vector2 screenPoint = (camera.WorldToScreenPoint(asteroids[i].position));
                rect = new Rect(screenPoint.x - halfAsteroidTextureSize, (Screen.height - screenPoint.y) - halfAsteroidTextureSize, asteroidTextureSize, asteroidTextureSize);
                GUI.DrawTexture(rect, asteroidTexture);
                lastDrawn.Add(rect);
            }
            positionsChanged = false;
        }
    }

    void Update()
    {
        if (!CirclePhysicsManager.singleton.initialized) { return; }    //wait until CirclePhysicsManager executes LateStart method

        //calcuate max and min coordinates between which asteroids will be rendered
        cameraMax = new Vector3(playerTransform.position.x + asteroidRenderDistance, playerTransform.position.y + asteroidRenderDistance);
        cameraMin = new Vector3(playerTransform.position.x - asteroidRenderDistance, playerTransform.position.y - asteroidRenderDistance);

        if (CirclePhysicsManager.singleton.simulate)
        {
            for (int i = 0; i < CirclePhysicsManager.numberOfAsteroids; i++)
            {
                asteroids[i].position += (asteroidVelocities[i] * Time.deltaTime);
            }
            positionsChanged = true;
        }
    }

    public void Restart()
    {
        //restart positions
        for (int i = 0; i < numberOfAsteroids; i++)
        {
            asteroids[i].position = startAsteroidsPositions[i];
            asteroids[i].detect = true;
        }
    }
}
