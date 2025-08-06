using System.Collections;
using UnityEngine;

public class Asteroid : MonoBehaviour, IDamage
{
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
    
    MeshRenderer meshRenderer;
    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        origColor = meshRenderer.material.color;
    }

    private void Start()
    {
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
