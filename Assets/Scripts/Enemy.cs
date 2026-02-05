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

    [SerializeField] EnemyHealthBar healthBar;  

    [Header("Feedback opcional")]
    // public GameObject deathEffect;

    [Header("Sistema de Audio")]
    [Tooltip("Déjalo vacío, se creará automáticamente")]
    public AudioSource audioSource;

    [Header("Configuración de Audio 3D")]
    [Tooltip("Distancia mínima donde el audio está a volumen máximo")]
    public float audioMinDistance = 5f;

    [Tooltip("Distancia máxima donde el audio deja de oírse")]
    public float audioMaxDistance = 20f;

    [Header("Sonidos de Patrulla")]
    [Tooltip("Sonidos que se reproducen aleatoriamente durante la patrulla")]
    public AudioClip[] patrolSounds;

    [Tooltip("Tiempo mínimo entre sonidos de patrulla")]
    public float minPatrolSoundInterval = 3f;

    [Tooltip("Tiempo máximo entre sonidos de patrulla")]
    public float maxPatrolSoundInterval = 8f;

    [Tooltip("Volumen de los sonidos de patrulla (0-1)")]
    [Range(0f, 1f)]
    public float patrolSoundVolume = 0.5f;

    [Header("Sonidos de Ataque")]
    [Tooltip("Sonidos que se reproducen al atacar")]
    public AudioClip[] attackSounds;

    [Tooltip("Volumen de los sonidos de ataque (0-1)")]
    [Range(0f, 1f)]
    public float attackSoundVolume = 0.8f;

    [Header("Sonidos de Muerte")]
    [Tooltip("Sonido que se reproduce al morir")]
    public AudioClip deathSound;

    [Tooltip("Volumen del sonido de muerte (0-1)")]
    [Range(0f, 1f)]
    public float deathSoundVolume = 1f;

    private float nextPatrolSoundTime;
    private bool isPatrolling;

    [Header("Flash al recibir ataque")]
    private EnemyDamageFlash damageFlash;

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

        healthBar = GetComponentInChildren <EnemyHealthBar>();

        // Configurar AudioSource automáticamente
        SetupAudioSource();

        damageFlash = GetComponent<EnemyDamageFlash>();
    }

    /// <summary>
    /// Configura el AudioSource automáticamente si no existe o lo actualiza si ya existe
    /// </summary>
    private void SetupAudioSource()
    {
        // Si no hay referencia, intentar obtener componente existente
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Si aún no existe, crear uno nuevo
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            //Debug.Log($"AudioSource creado automáticamente en {gameObject.name}");
        }

        // Configurar propiedades para audio 3D espacial
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; 
        audioSource.minDistance = audioMinDistance;
        audioSource.maxDistance = audioMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f; // Desactivar efecto doppler

        // Configuración adicional recomendada
        audioSource.loop = false;
        audioSource.priority = 128; // 
    }

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.UpdateHealthBar (currentHealth, maxHealth); 

        doorManager = FindFirstObjectByType<LevelDoorManager>();
        potionSpawner = FindFirstObjectByType<HealthPotionSpawner>();

        // Inicializar timer de sonido de patrulla
        nextPatrolSoundTime = Time.time + Random.Range(minPatrolSoundInterval, maxPatrolSoundInterval);
    }

    // Método para resetear el enemigo cuando vuelve del pool
    public void ResetEnemy()
    {
        currentHealth = maxHealth;
        isDead = false;
        isAttacking = false;
        walkPointSet = false;
        isPatrolling = false;

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

        // Detener audio
        if (audioSource != null)
            audioSource.Stop();

        // Resetear timer de patrulla
        nextPatrolSoundTime = Time.time + Random.Range(minPatrolSoundInterval, maxPatrolSoundInterval);
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
        isPatrolling = true;
        agent.isStopped = false;

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

        // Reproducir sonidos de patrulla aleatoriamente
        PlayPatrolSound();
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
        isPatrolling = false;
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // Animación de movimiento basada en velocidad real
        float normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("moverse", normalizedSpeed);
    }

    private void AttackPlayer()
    {
        isPatrolling = false;

        // El enemigo se queda quieto
        agent.isStopped = true;
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

        // Reproducir sonido de ataque
        PlayAttackSound();

        yield return new WaitForSeconds(attackDelay);

        // Realizar el daño
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
                //Debug.Log("¡Ataca! Daño aplicado: " + damageAmount);
            }
        }

        // Desactivar animación de ataque
        animator.SetBool("atacar", false);
        agent.isStopped = false;

        yield return new WaitForSeconds(timeBetweenAttacks);

        isAttacking = false;
        attackCoroutine = null;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        healthBar.UpdateHealthBar (currentHealth, maxHealth); 
        //Debug.Log(gameObject.name + " recibió " + amount + " de daño. Vida restante: " + currentHealth);

        if (damageFlash !=null)
        {
            damageFlash.Flash();
        }
        
        if (currentHealth <= 0)
            Die();
    }

    //MUERTE
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        //Debug.Log(gameObject.name + " ha muerto.");

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

        // Reproducir sonido de muerte
        PlayDeathSound();

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

        if (audioSource != null && deathSound != null)
        {
           audioSource.PlayOneShot(deathSound, deathSoundVolume);
        }

        yield return new WaitForSeconds(Mathf.Max(timeBeforeDying, deathSound.length));

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

    // MÉTODOS DE AUDIO

    /// <summary>
    /// Reproduce sonidos de patrulla de forma aleatoria a intervalos
    /// </summary>
    private void PlayPatrolSound()
    {
        if (patrolSounds == null || patrolSounds.Length == 0)
            return;

        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource no encontrado en {gameObject.name}");
            return;
        }

        if (Time.time >= nextPatrolSoundTime && !audioSource.isPlaying)
        {
            // Seleccionar un sonido aleatorio
            AudioClip randomClip = patrolSounds[Random.Range(0, patrolSounds.Length)];

            if (randomClip != null)
            {
                audioSource.PlayOneShot(randomClip, patrolSoundVolume);
            }

            // Establecer el próximo tiempo de reproducción
            nextPatrolSoundTime = Time.time + Random.Range(minPatrolSoundInterval, maxPatrolSoundInterval);
        }
    }

    /// <summary>
    /// Reproduce un sonido aleatorio de ataque
    /// </summary>
    private void PlayAttackSound()
    {
        if (attackSounds == null || attackSounds.Length == 0)
            return;

        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource no encontrado en {gameObject.name}");
            return;
        }

        // Seleccionar un sonido aleatorio
        AudioClip randomClip = attackSounds[Random.Range(0, attackSounds.Length)];

        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip, attackSoundVolume);
        }
    }

    /// <summary>
    /// Reproduce el sonido de muerte
    /// </summary>
    private void PlayDeathSound()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
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