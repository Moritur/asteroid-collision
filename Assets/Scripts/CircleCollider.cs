using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CircleCollider
{

    public event Action OnCollision;
    public bool isAsteroid;
    public bool detect = true;
    public bool isPlayer;

    [HideInInspector]
    public Transform transform;
    Vector3 currentPosition;
    [HideInInspector]
    public Vector3 position
    {
        //get {
        //    if (isAsteroid && !isPlayer)
        //    {
        //        return currentPosition;
        //    }
        //    else
        //    {
        //        return transform.position;
        //    }
        //    }
        get => currentPosition;
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

    public void Collide() => OnCollision();

    public void CachePosition() => currentPosition = transform.position;

    //for asteroids
    public CircleCollider(Vector3 position)
    {
        isAsteroid = true;
        isPlayer = false;
        OnCollision += AsteroidCollision;
        this.position = position;
        CirclePhysicsManager.singleton.AddCircleCollider(this);
    }

    //for bullets and player
    public CircleCollider(Action action, bool isPlayer, Transform transform)
    {
        isAsteroid = isPlayer;
        this.isPlayer = isPlayer;
        OnCollision += action;
        this.transform = transform;
        CirclePhysicsManager.singleton.AddCircleCollider(this);
    }

    void AsteroidCollision()
    {
        detect = false;
        CirclePhysicsManager.singleton.RespawnAsteroid(this);
    }


}