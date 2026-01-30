using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

public class Spawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenSpawns = 5f;
    [SerializeField] private int maxEnemiesPerLevel = 3; // Límite editable desde el inspector

    private float timeSinceLastSpawn;
    [SerializeField] private Enemy enemyPrefab;
    private IObjectPool<Enemy> enemyPool;
    private int currentActiveEnemies = 0;

    private void Awake()
    {
        enemyPool = new ObjectPool<Enemy>(CreateEnemy, OnGet, OnRelease);
    }

    private void OnGet(Enemy enemy)
    {
        // Resetear el estado del enemigo
        enemy.gameObject.SetActive(true);
        enemy.ResetEnemy();

        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        enemy.transform.position = randomSpawnPoint.position;
        currentActiveEnemies++;
    }

    private void OnRelease(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
        currentActiveEnemies--;
    }

    private Enemy CreateEnemy()
    {
        Enemy enemy = Instantiate(enemyPrefab);
        enemy.SetPool(enemyPool);
        return enemy;
    }

    void Update()
    {
        // Solo hacer spawn si no hemos alcanzado el límite y ha pasado el tiempo necesario
        if (currentActiveEnemies < maxEnemiesPerLevel && Time.time > timeSinceLastSpawn)
        {
            enemyPool.Get();
            timeSinceLastSpawn = Time.time + timeBetweenSpawns;
        }
    }
}