using UnityEngine;

public class HealthPotionSpawner : MonoBehaviour
{
    [Header("Configuración de spawn")]
    [SerializeField] private GameObject healthPotionPrefab;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private bool spawnOnAllEnemiesDead = true;
    
    [Header("Rotación del modelo")]
    [Tooltip("Activa para usar rotación personalizada")]
    [SerializeField] private bool useCustomRotation = true;
    
    [Tooltip("Rotación al spawnear (X=90 para vertical, Y para girar horizontalmente, Z para inclinar)")]
    [SerializeField] private Vector3 spawnRotation = new Vector3(90f, 0f, 0f); // Vertical por defecto
    
    private int enemyCount;
    private bool potionSpawned = false;

    void Start()
    {
        // Cuenta todos los enemigos al inicio
        enemyCount = GameObject.FindGameObjectsWithTag("enemy").Length;
        Debug.Log("Enemigos detectados al inicio: " + enemyCount);
    }

    public void EnemyDefeated()
    {
        enemyCount--;
        Debug.Log("Enemigos restantes: " + enemyCount);

        // Cuando no quedan enemigos y aún no se ha spawneado la poción
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
        Quaternion spawnRot = useCustomRotation ? Quaternion.Euler(spawnRotation) : Quaternion.identity;
        
        GameObject potion = Instantiate(healthPotionPrefab, spawnPos, spawnRot);
        potionSpawned = true;
        
        Debug.Log("¡Poción de vida spawneada en " + spawnPos + " con rotación " + spawnRotation + "!");
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