using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Asteroid : MonoBehaviour, IDamage
{
    [Header("Movement")]
    [SerializeField] float moveSpeed;
    public bool canMove;

    [Header("Asteroid Settings")]
    [SerializeField] public float health;
    [SerializeField] AsteroidSO asteroid;

    [Header("Resource")]
    [SerializeField] int minAmount;
    [SerializeField] int maxAmount;
    [SerializeField] int bonusAmount;

    [Header("Color")]
    [SerializeField] Color hitColor = Color.red;
    private Color origColor;

    [Header("Sound")]
    [SerializeField] SoundModulation soundModulation;
    [SerializeField] AudioSource damageSource;

    [Header("Border")]
    [SerializeField] Vector2 borderLimits;

    [Header("Debug")]
    [SerializeField] bool debug;

    private Transform graphicTransform;
    private MeshRenderer meshRenderer;
    private Vector3 rotationAxis;
    private float angularSpeed;
    private Rigidbody rb;

    // Instance event (FIX: no longer static)
    public event Action OnAsteroidDestroyed;

    public void Initialize(AsteroidSO data)
    {
        asteroid = data;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            origColor = meshRenderer.material.color;
            graphicTransform = meshRenderer.transform;
        }
        else
        {
            graphicTransform = transform;
        }
    }

    private void Start()
    {
        canMove = true;

        float scaleFactor = 1f;
        switch (asteroid.asteroidSize)
        {
            case AsteroidSO.Size.Medium:
                scaleFactor = 1.5f;
                break;
            case AsteroidSO.Size.Large:
                scaleFactor = 2f;
                break;
        }
        transform.localScale *= scaleFactor;

        moveSpeed = UnityEngine.Random.Range(asteroid.minMoveSpeed, asteroid.maxMoveSpeed);

        rb.linearVelocity = transform.forward * moveSpeed;

        rotationAxis = UnityEngine.Random.onUnitSphere;
        angularSpeed = UnityEngine.Random.Range(asteroid.minRotSpeed, asteroid.maxRotSpeed);
        graphicTransform.localRotation = UnityEngine.Random.rotation;

        health = asteroid.health;
        minAmount = asteroid.minAmount;
        maxAmount = asteroid.maxAmount;
        bonusAmount = asteroid.bonusAmount;
    }

    private void Update()
    {
        if (debug && Input.GetKeyDown(KeyCode.F3))
            TakeDamage(1);

        graphicTransform.Rotate(rotationAxis, angularSpeed * Time.deltaTime, Space.Self);

        if (transform.position.x > borderLimits.x
            || transform.position.x < 0
            || transform.position.z > borderLimits.y
            || transform.position.z < 0)
            DestroyAsteroid();

        if (!canMove)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Asteroid")) return;

        // Get the contact point normal
        ContactPoint contact = collision.contacts[0];
        Vector3 bounceDir = contact.normal;

        // Apply force away from the collision point
        float bounceForce = rb.linearVelocity.magnitude * 1.5f; // scale with speed
        rb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);
    }

    public void TakeDamage(float damage)
    {
        soundModulation.ModulateSound(UnityEngine.Random.Range(0.8f, 1.2f));
        damageSource.Play();

        health -= damage;
        StartCoroutine(FlashRed());

        int amount = UnityEngine.Random.Range(minAmount, maxAmount);
        if (health <= 0)
        {
            ResourceManager.instance.AddResource(asteroid.resource.resourceType, amount + bonusAmount);

            // Raise the instance event
            OnAsteroidDestroyed?.Invoke();

            Destroy(gameObject);
        }
        else
        {
            ResourceManager.instance.AddResource(asteroid.resource.resourceType, amount);
        }
    }

    public void DestroyAsteroid()
    {
        Destroy(gameObject);
    }

    private IEnumerator FlashRed()
    {
        if (meshRenderer != null)
            meshRenderer.material.color = hitColor;

        yield return new WaitForSeconds(0.1f);

        if (meshRenderer != null)
            meshRenderer.material.color = origColor;
    }
}
