using UnityEngine;

public class HealthPotionSpawner : MonoBehaviour
{
   [Header ("Configuración de spawn")]
   [SerializeField] private GameObject healthPotionPrefab;
   [SerializeField] private Transform spawnPosition;
   [SerializeField] private bool spawnOnAllEnemiesDead = true;

   private int enemyCount;
   private bool potionSpawned = false;

   void Start()
   {
    enemyCount = GameObject.FindGameObjectsWithTag("enemy").Length;
    Debug.Log("Enemigos detectados al inicio");
   }

   public void EnemyDefeated()
   {

    enemyCount--;
    Debug.Log("Enemigos restantes: " + enemyCount);

    if (enemyCount <= 0 && !potionSpawned && spawnOnAllEnemiesDead)
    {
        SpawnHealthPotion();
    }
   }

   private void SpawnHealthPotion()
    {
        if (healthPotionPrefab == null)
        {
            Debug.LogError("No se asignó el prefab de la poción de vida!");
            return;
        }

        Vector3 spawnPos = spawnPosition != null ? spawnPosition.position : transform.position;
        
        GameObject potion = Instantiate(healthPotionPrefab, spawnPos, Quaternion.identity);
        potionSpawned = true;
        
        Debug.Log("¡Poción de vida spawneada en " + spawnPos + "!");
    }

    // Método opcional para spawnear la poción manualmente
    public void ForceSpawnPotion()
    {
        if (!potionSpawned)
        {
            SpawnHealthPotion();
        }
    }
}
