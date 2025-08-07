using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Asteroid : MonoBehaviour, IDamage
{
    [Header("Movement")]
    [SerializeField] float moveSpeed;

    [Header("Asteroid Settings")]
    [SerializeField] float health;
    [SerializeField] AsteroidSO asteroid;

    [Header("Resource")]
    [SerializeField] int minAmount;
    [SerializeField] int maxAmount;
    [SerializeField] int bonusAmount;

    [Header("Color")]
    [SerializeField] Color origColor;
    [SerializeField] Color hitColor = Color.red;

    [Header("Debug")]
    [SerializeField] bool debug;

    private Vector3 rotationAxis;
    private float angularSpeed;

    private Transform parent;
    
    MeshRenderer meshRenderer;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        origColor = meshRenderer.material.color;
    }

    private void Start()
    {
        // Move Values for the asteroid
        moveSpeed = Random.Range(asteroid.minMoveSpeed, asteroid.maxMoveSpeed);
        transform.localRotation = Random.rotation;
        rotationAxis = Random.onUnitSphere;
        angularSpeed = Random.Range(asteroid.minRotSpeed, asteroid.maxRotSpeed);
        //rotationSpeed = new Vector3(
        //    Random.Range(asteroid.minRotSpeed.x, asteroid.maxRotSpeed.x),
        //    Random.Range(asteroid.minRotSpeed.y, asteroid.maxRotSpeed.y),
        //    Random.Range(asteroid.minRotSpeed.z, asteroid.maxRotSpeed.z)
        //    );

        // Caching the parent on start
        parent = transform.parent;

        // Asteroid Values
        health = asteroid.health;
        minAmount = asteroid.minAmount;
        maxAmount = asteroid.maxAmount;
        bonusAmount = asteroid.bonusAmount;
    }

    private void Update()
    {
        if(debug)
        {
            if(Input.GetKeyDown(KeyCode.F3))
            {
                TakeDamage(1);
            }
        }

        parent.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        transform.Rotate(rotationAxis ,angularSpeed * Time.deltaTime, Space.Self);
    }
    public void TakeDamage(float damage)
    {
        health -= damage;
        StartCoroutine(FlashRed());
        if(health <= 0)
        {

            ResourceManager.instance.AddResource(asteroid.resource.resourceType, Random.Range(minAmount, maxAmount) + bonusAmount);
            Destroy(gameObject);
        }
        else
        {
            ResourceManager.instance.AddResource(asteroid.resource.resourceType, Random.Range(minAmount, maxAmount));
        }
    }

    private IEnumerator FlashRed()
    {
        meshRenderer.material.color = hitColor;
        yield return new WaitForSeconds(0.1f);
        meshRenderer.material.color = origColor;


    }
}
