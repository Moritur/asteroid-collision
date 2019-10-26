using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour
{
    [HideInInspector]
    new public Transform transform;
    public CircleCollider circleCollider { get; private set; }
    public SpriteRenderer spriteRenderer { get; private set; }
    public bool disableRenderer { get; private set; }

    void Awake()
    {
        transform = GetComponent<Transform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        circleCollider = new CircleCollider(OnCollision, false, transform);
    }

    public void CachePosition() => circleCollider.CachePosition();

    public void OnCollision()
    {
        GameManager.singleton.AddScore();
        disableRenderer = true;
        circleCollider.detect = false;
    }

    public void DisableRendererComponent()
    {
        spriteRenderer.enabled = false;
        disableRenderer = false;
    }

}