using System.Collections;
using UnityEngine;

public class Turret : MonoBehaviour, IDamage
{
    public float detectionRange = 10f; //  Adjust this in the Inspector
    public float fireRate = 1f;       //  Adjust this in the Inspector (bullets per second)
    public GameObject projectilePrefab; // Assign your bullet/projectile prefab here
    public Transform firePoint;       //  Specify where projectiles should spawn

    private GameObject currentTarget;
    private float nextFireTime;
    public UnitSO stats;

    public UnitSO[] upgradeLevels;
    private UnitSO upgradeStats;
    private int upgradeIndex = 0;

    [SerializeField] Renderer model;
    [SerializeField] GameObject fragmentModel;

    public float health;

    private Color origColor;

    void Start()
    {
        origColor = model.material.color;
        health = stats.unitHealth;
    }
    void Update()
    {
        FindNearestEnemy();
        if (currentTarget != null && Time.time >= nextFireTime)
        {
            FireAtTarget();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy <= detectionRange && distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }
        currentTarget = nearestEnemy;
    }

    void FireAtTarget()
    {
        //  Make the turret face the target
        Vector3 directionToTarget = currentTarget.transform.position - firePoint.position;
        firePoint.rotation = Quaternion.LookRotation(directionToTarget);

        //  Instantiate and launch the projectile
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        //  Add logic to handle projectile movement and collision in a separate script attached to your projectile prefab.
    }

    public void TakeDamage(float amount)
    {
        
        health -= amount;
        StartCoroutine(FlashRed());

        if (health <= 0)
        {
            if (fragmentModel != null)
                fragmentModel.SetActive(true);
            else
                Debug.Log("fragmentModel missing");

            Destroy(gameObject);
        }
    }

    IEnumerator FlashRed()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material.color = origColor;
    }

    public void Upgrade()
    {
        // Add pricing to the if chech as well before going through
        if (upgradeLevels.Length > upgradeIndex)
        {
            
            health = upgradeLevels[upgradeIndex].unitHealth;
            Damage damage = projectilePrefab.GetComponent<Damage>();
            damage.damageAmount = upgradeLevels[upgradeIndex].attackDamage;
            upgradeIndex++;
        }
    }
}