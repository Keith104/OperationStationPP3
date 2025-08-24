using System.Collections;
using UnityEngine;

public class Wall : MonoBehaviour, IDamage
{
    [SerializeField] UnitSO stats;
    public float health;

    [SerializeField] Renderer model;
    [SerializeField] GameObject fragmentModel;

    private Color origColor;
    private void Start()
    {
        health = stats.unitHealth;
        origColor = model.material.color;
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
}
