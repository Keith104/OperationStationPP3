using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty", menuName = "OperationStation/New Difficulty")]
public class DifficultySO : ScriptableObject
{
    [Header("Difficulty Name")]
    public string difficultyName;

    [Header("Enemy Stat Multipliers")]
    public float health;
    public float damage;
    public int attackCooldown;
}
