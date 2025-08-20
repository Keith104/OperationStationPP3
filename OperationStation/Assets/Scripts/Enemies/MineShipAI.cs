using UnityEngine;
using UnityEngine.UIElements;
using static System.Collections.Specialized.BitVector32;

public class MineShipAI : EnemyAI
{
    
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    public override void Attack()
    {
        if(station != null)
        {
            agent.SetDestination(station.transform.position);
            shootTimer += Time.deltaTime;
        }
    }
}
