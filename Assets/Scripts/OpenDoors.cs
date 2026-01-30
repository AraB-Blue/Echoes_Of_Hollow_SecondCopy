using UnityEngine;

public class OpenDoors : MonoBehaviour
{
    public Animator animator;
    private int enemyCount;
    void Start()
    {
        animator = GetComponent<Animator>();
        enemyCount = GameObject.FindGameObjectsWithTag("enemy").Length;
    }

    public void EnemyDefeated()
    {
        enemyCount--;

        if(enemyCount<=0)
        {
            animator.SetTrigger("OpenDoor");
            
        }
    }
}
