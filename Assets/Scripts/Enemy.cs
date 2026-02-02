using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Atributos del enemigo")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Feedback opcional")]
    //public GameObject deathEffect;

    [Header("IA")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;
    private IObjectPool<Enemy> enemyPool;

    public void SetPool(IObjectPool<Enemy> pool)
    {
        enemyPool = pool;
    }

    // Patrulla
    public Vector3 walkPoint;
    private bool walkPointSet;
    public float walkPointRange;

    // Ataque
    public float timeBetweenAttacks = 8f;
    public float damageAmount = 10f;
    private bool isAttacking;
    private Coroutine attackCoroutine;
    public float attackDelay;

    // Estados
    public float sightRange = 10f, attackRange = 2f;
    private bool playerInSightRange, playerInAttackRange;
    public float timeBeforeDying = 3f;
    private bool isDead;

    Animator animator;

    private LevelDoorManager doorManager;
    private HealthPotionSpawner potionSpawner;

    private void Awake()
    {
        // Busca el jugador automáticamente si no está asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("No se encontró ningún GameObject llamado 'Player'. Asignalo en el Inspector.");
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError("El Enemy necesita un NavMeshAgent. Agrega uno al GameObject.");

        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHealth = maxHealth;

        doorManager = FindFirstObjectByType<LevelDoorManager>();
        potionSpawner = FindFirstObjectByType<HealthPotionSpawner>();
    }

    // Método para resetear el enemigo cuando vuelve del pool
    public void ResetEnemy()
    {
        currentHealth = maxHealth;
        isDead = false;
        isAttacking = false;
        walkPointSet = false;

        // Detener coroutines activas
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        // Reactivar componentes
        agent.enabled = true;
        GetComponent<Collider>().enabled = true;

        // Resetear animador completamente
        if (animator != null)
        {
            animator.SetFloat("moverse", 0f);
            animator.SetBool("atacar", false);
            animator.SetBool("morirse", false);
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Comprueba si el jugador está en rango
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        // Lógica de estados
        if (!playerInSightRange && !playerInAttackRange)
        {
            Patroling();
        }
        else if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer();
        }
        else if (playerInAttackRange)
        {
            AttackPlayer();
        }
    }

    //PATRULLA
    private void Patroling()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        // Llegó al punto - buscar nuevo punto inmediatamente
        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;
        }

        // Animación de movimiento basada en velocidad real
        float normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("moverse", normalizedSpeed);
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        // Verifica que el punto esté sobre el suelo
        if (Physics.Raycast(walkPoint, Vector3.down, 2f, whatIsGround))
            walkPointSet = true;
    }

    //SEGUIR JUGADOR
    private void ChasePlayer()
    {
        agent.SetDestination(player.position);

        // Animación de movimiento basada en velocidad real
        float normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("moverse", normalizedSpeed);
    }

    private void AttackPlayer()
    {
        // El enemigo se queda quieto
        agent.SetDestination(transform.position);
        animator.SetFloat("moverse", 0f);

        // Mira al jugador
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (!isAttacking)
        {
            attackCoroutine = StartCoroutine(AttackRoutine());
        }
    }

    //ATAQUE
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Activar animación de ataque con BOOL
        animator.SetBool("atacar", true);

        yield return new WaitForSeconds(attackDelay);

        // Realizar el daño
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                Debug.Log("¡Ataca! Daño aplicado: " + damageAmount);
            }
        }

        // Desactivar animación de ataque
        animator.SetBool("atacar", false);

        yield return new WaitForSeconds(timeBetweenAttacks);

        isAttacking = false;
        attackCoroutine = null;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + " recibió " + amount + " de daño. Vida restante: " + currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    //MUERTE
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " ha muerto.");

        // Detener coroutine de ataque si está activa
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        /*if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);*/

        StartCoroutine(DyingCoroutine());

        // Notificar a ambos sistemas
        if (doorManager != null)
            doorManager.EnemyDefeated();

        if (potionSpawner != null)
            potionSpawner.EnemyDefeated();
    }

    private IEnumerator DyingCoroutine()
    {
        // Activar animación de muerte con BOOL
        animator.SetBool("morirse", true);

        yield return new WaitForSeconds(timeBeforeDying);

        // Desactivar animación (aunque ya no importa porque se va a destruir/liberar)
        animator.SetBool("morirse", false);

        // Usar pool si está disponible, sino destruir
        if (enemyPool != null)
        {
            enemyPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Visualizar rangos en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}