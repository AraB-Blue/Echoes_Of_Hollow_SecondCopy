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

    //pausa patrulla
    private bool isIdling = false;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float idleChance = 0.4f;

    // Ataque
    public float timeBetweenAttacks = 8f;
    public float damageAmount = 10f;
    private bool isAttacking;
    public float attackDelay;

    // Estados
    public float sightRange = 10f, attackRange = 2f;
    private bool playerInSightRange, playerInAttackRange;
    public float timeBeforeDying = 3f;
    private bool isDead;

    Animator animator;

    private LevelDoorManager doorManager;
    
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
    }

    // Método para resetear el enemigo cuando vuelve del pool
    public void ResetEnemy()
    {
        currentHealth = maxHealth;
        isDead = false;
        isAttacking = false;
        isIdling = false;
        walkPointSet = false;

        // Reactivar componentes
        agent.enabled = true;
        GetComponent<Collider>().enabled = true;

        // Resetear animador si es necesario
        if (animator != null)
        {
            animator.SetFloat("moverse", 0f);
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
        if (isIdling)
        {
            animator.SetFloat("moverse", 0f);
            return;
        }

        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        // Llegó al punto
        if (distanceToWalkPoint.magnitude < 1f)
        {
            walkPointSet = false;

            if (Random.value < idleChance)
            {
                StartCoroutine(IdleRoutine());
            }
        }

        animator.SetFloat("moverse", 0.1f);
    }

    private IEnumerator IdleRoutine()
    {
        isIdling = true;

        //tiempo aleatorio de la idle
        float idleTime = Random.Range(minIdleTime, maxIdleTime);

        Debug.Log("Enemigo en pausa por " + idleTime + " segundos");

        yield return new WaitForSeconds(idleTime);

        isIdling = false;
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
        animator.SetFloat("moverse", 0.1f);
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
            StartCoroutine(AttackRoutine());
        }
    }

    //ATAQUE
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        animator.SetTrigger("atacar");

        yield return new WaitForSeconds(attackDelay);

        Debug.Log("¡Ataca!");

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }

        yield return new WaitForSeconds(timeBetweenAttacks);

        isAttacking = false;
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

        agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        /*if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);*/

        StartCoroutine(DyingCoroutine());

        doorManager.EnemyDefeated();

       
    }

    private IEnumerator DyingCoroutine()
    {
        animator.SetTrigger("morirse");

        yield return new WaitForSeconds(timeBeforeDying);

        Destroy(gameObject);

        // CORRECCIÓN: Usar enemyPool en lugar de IObjectPool
        // Y NO destruir el GameObject, solo devolverlo al pool
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