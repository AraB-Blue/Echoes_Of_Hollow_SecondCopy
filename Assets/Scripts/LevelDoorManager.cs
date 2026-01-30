using System.Collections;
using UnityEngine;

public class LevelDoorManager : MonoBehaviour
{
    [Header ("Puertas")]
    public Animator[] doorAnimators;

    [Header("Poci�n de vida")]
    private int enemyCount;
    private bool levelCleared = false;

    private void Start()
    {
        enemyCount = GameObject.FindGameObjectsWithTag("enemy").Length;

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
    }

}
