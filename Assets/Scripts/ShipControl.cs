using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipControl : MonoBehaviour
{
    const float shootingSpeed = 0.5f;
    const float bulletLifetime = 3f;
    //max number of bullets in game can be calculated from shootingSpeed and bulletLifetime
    public const int maxBullets = (((bulletLifetime % shootingSpeed) == 0) ? (int)(bulletLifetime / shootingSpeed) : (int)((bulletLifetime / shootingSpeed) + 1));
    const float bulletSpeed = 8f;
#pragma warning disable CS0649
    [SerializeField]
    float movementSpeed;
    [SerializeField]
    float rotationSpeed;
    [SerializeField]
    GameObject bullet;
#pragma warning restore CS0649

    bool isDead = false;
    Bullet[] bulletComponents = new Bullet[maxBullets];
    int currentBullet = 0;
    Vector3 startPosition;
    Quaternion startRotation;
    SpriteRenderer spriteRenderer;
    new public Transform transform { get; private set; }
    public CircleCollider circleCollider { get; private set; }

    void Awake()
    {
        transform = GetComponent<Transform>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        for(int i = 0; i<maxBullets; i++)
        {
            bulletComponents[i] = Instantiate(bullet).GetComponent<Bullet>();
            bulletComponents[i].spriteRenderer.enabled=false;
        }
    }

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        StartCoroutine(Shooting());
        GameManager.singleton.RestartGame += Restart;
        circleCollider = new CircleCollider(OnCollision, true, transform);
    }

    void Update()
    {
        if (!isDead)
        {
            Movement();

            for(int i=0; i< maxBullets; i++)
            {
                bulletComponents[i].transform.Translate(Vector3.up*bulletSpeed*Time.deltaTime);
            }
        }
    }

    void Movement()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(Vector3.up * movementSpeed * Time.deltaTime);
        }
        if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }else if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }

    public void OnCollision()
    {
            Die();
    }

    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        spriteRenderer.enabled = false;
        GameManager.singleton.GameOver();
    }

    void Restart()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        spriteRenderer.enabled = true;
        foreach (Bullet b in bulletComponents)
        {
            b.circleCollider.detect = false;
            b.spriteRenderer.enabled = false;
            b.transform.position = transform.position;
            b.transform.rotation = transform.rotation;
        }
        isDead = false;
        StartCoroutine(Shooting());
    }

    IEnumerator Shooting()
    {
        yield return new WaitForSeconds(shootingSpeed);
        while (true)
        {
            bulletComponents[currentBullet].circleCollider.detect = false;
            bulletComponents[currentBullet].spriteRenderer.enabled=false;
            bulletComponents[currentBullet].transform.position = transform.position;
            CirclePhysicsManager.singleton.AssignSameCellAsPlayer(bulletComponents[currentBullet].circleCollider);
            bulletComponents[currentBullet].transform.rotation = transform.rotation;
            bulletComponents[currentBullet].spriteRenderer.enabled=true;
            bulletComponents[currentBullet].circleCollider.detect = true;

            if (currentBullet >= (maxBullets - 1))
            {
                currentBullet = 0;
            }
            else
            {
                currentBullet++;
            }
            yield return new WaitForSeconds(shootingSpeed);
        }
    }

}
