using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CircleCollider
{

    public UnityEvent OnCollision;
    public bool isAsteroid;
    public bool detect = true;
    public bool isPlayer;

    [HideInInspector]
    public Transform transform;
    Vector3 currentPosition;
    [HideInInspector]
    public Vector3 position
    {
        get {
            if (isAsteroid && !isPlayer)
            {
                return currentPosition;
            }
            else
            {
                return transform.position;
            }
            }
        set
        {
            if (isAsteroid && !isPlayer)
            {
                currentPosition = value;
            }
            else
            {
                transform.position = value;
            }
        }
    }

    //for asteroids
    public CircleCollider(Vector3 position)
    {
        isAsteroid = true;
        isPlayer = false;
        OnCollision = new UnityEvent();
        OnCollision.AddListener(AsteroidCollision);
        this.position = position;
        CirclePhysicsManager.singleton.AddCircleCollider(this);
    }

    //for bullets and player
    public CircleCollider(UnityAction action, bool isPlayer, Transform transform)
    {
        isAsteroid = isPlayer;
        this.isPlayer = isPlayer;
        OnCollision = new UnityEvent();
        OnCollision.AddListener(action);
        this.transform = transform;
        CirclePhysicsManager.singleton.AddCircleCollider(this);
    }

    void AsteroidCollision()
    {
        detect = false;
        CirclePhysicsManager.singleton.RespawnAsteroid(this);
    }


}