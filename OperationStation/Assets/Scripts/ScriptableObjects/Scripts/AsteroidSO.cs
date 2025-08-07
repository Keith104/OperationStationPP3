using UnityEngine;

[CreateAssetMenu(fileName = "New Asteroid", menuName = "OperationStation/New Asteroid")]
public class AsteroidSO : ScriptableObject
{
    public enum Size
    {
        Small,
        Medium,
        Large
    }

    public Size asteroidSize;
    public GameObject asteroidObject;

    public float health;

    [Header("Resource")]
    public ResourceSO resource;
    public int minAmount;
    public int maxAmount;
    public int bonusAmount;

    [Header("Movement Settings")]
    public float minMoveSpeed;
    public float maxMoveSpeed;

    [Header("Rotation Settings")]
    public Vector3 minRotSpeed;
    public Vector3 maxRotSpeed;

}
