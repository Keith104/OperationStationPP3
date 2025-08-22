using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty", menuName = "OperationStation/New Difficulty")]
public class DifficultySO : ScriptableObject
{
    [Header("Difficulty")]
    public string difficultyName;
    public string difficultyDescription;
    public bool isLocked;

    [Header("UI")]
    public Color uiColor;

    [Header("Enemy Stat Multipliers")]
    public float health;
    public float damage;
    public int attackCooldown;
}
