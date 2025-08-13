using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "OperationStation/New Enemy")]
public class EnemiesSO : ScriptableObject
{
    public enum EnemyType
    {
        BowFighter,
        VerticalWing,
        DOGDestroyer,
        SuperDOGDestroyer
    }

    public EnemyType enemyType;
    public GameObject enemyObject;

    public float health;

    [Header("Damage Data")]
    public GameObject bullet;
    public float damageAmount;
    public float attackCooldown;
}
