using System.Collections;
using UnityEngine;

public class LevelDoorManager : MonoBehaviour
{
    [Header ("Puertas")]
    public Animator[] doorAnimators;

    [Header("Poción de vida")]
    public GameObject healthPotion;
    private int enemyCount;
    private bool levelCleared = false;

    private void Start()
    {
        enemyCount = GameObject.FindGameObjectsWithTag("enemy").Length;

        if (healthPotion !=null)
        {
            healthPotion.SetActive(false); //esta oculta al principio
        }
    }

    public void EnemyDefeated()
    {
        if (levelCleared) return;

        enemyCount--;

        if(enemyCount <=0)
        {
            LevelCompleted();
        }
    }

    void LevelCompleted()
    {
        levelCleared = true;

        //puertas

        foreach (Animator anim in doorAnimators)
        {
            anim.SetTrigger("OpenDoor");
        }

        //pocion

        if (healthPotion !=null)
        {
            StartCoroutine(spawnPotion());
            
        }
    }

    private IEnumerator spawnPotion()
    {
        yield return new WaitForSeconds(2);
        
        healthPotion.SetActive(true);


    }


   

}
