using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty", menuName = "OperationStation/New Difficulty")]
public class DifficultySO : ScriptableObject
{
    [Header("Difficulty")]
    public string difficultyName;
    public bool isLocked;

    [Header("Enemy Stat Multipliers")]
    public float health;
    public float damage;
    public int attackCooldown;
}
