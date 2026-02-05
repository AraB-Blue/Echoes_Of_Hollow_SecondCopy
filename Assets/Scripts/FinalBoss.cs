using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class FinalBoss : MonoBehaviour, IDamageable
{
    [Header("Atributos del jefe final")]
    public int maxHealth = 100;
    private int currentHealth;

    [SerializeField] FinalBossHealthBar healthBar;

    [Header("Sistema de Audio")]
    [Tooltip("Déjalo vacío, se creará automáticamente")]
    public AudioSource audioSource;

    [Header("Configuración de Audio 3D")]
    [Tooltip("Distancia mínima donde el audio está a volumen máximo")]
    public float audioMinDistance = 5f;

    [Tooltip("Distancia máxima donde el audio deja de oírse")]
    public float audioMaxDistance = 25f;

    [Header("Sonidos de Patrulla")]
    [Tooltip("Sonidos que se reproducen aleatoriamente durante la patrulla")]
    public AudioClip[] patrolSounds;

    [Tooltip("Tiempo mínimo entre sonidos de patrulla")]
    public float minPatrolSoundInterval = 4f;

    [Tooltip("Tiempo máximo entre sonidos de patrulla")]
    public float maxPatrolSoundInterval = 10f;

    [Tooltip("Volumen de los sonidos de patrulla (0-1)")]
    [Range(0f, 1f)]
    public float patrolSoundVolume = 0.6f;

    [Header("Sonidos de Ataque 1")]
    [Tooltip("Sonidos que se reproducen al usar ataque 1")]
    public AudioClip[] attack1Sounds;

    [Tooltip("Volumen de los sonidos de ataque 1 (0-1)")]
    [Range(0f, 1f)]
    public float attack1SoundVolume = 0.8f;

    [Header("Sonidos de Ataque 2")]
    [Tooltip("Sonidos que se reproducen al usar ataque 2")]
    public AudioClip[] attack2Sounds;

    [Tooltip("Volumen de los sonidos de ataque 2 (0-1)")]
    [Range(0f, 1f)]
    public float attack2SoundVolume = 1f;

    [Header("Sonidos de Muerte")]
    [Tooltip("Sonido que se reproduce al morir")]
    public AudioClip deathSound;

    [Tooltip("Volumen del sonido de muerte (0-1)")]
    [Range(0f, 1f)]
    public float deathSoundVolume = 1f;

    private float nextPatrolSoundTime;
    private bool isPatrolling;

    [Header("IA")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;

    // Patrulla 
    public Vector3 walkPoint;
    private bool walkPointSet;
    public float walkPointRange = 15f;

    public bool canMove = true;

    //ataque
    public float timeBetweenAttacks = 8f;

    //ataque1
    public float attack1Damage = 10f;
    public float attack1Delay = 0.5f;
    public float attack1Range = 2f;

    //ataque 2
    public float attack2Damage = 20f;
    public float attack2Delay = 1f;
    public float attack2Range = 3f;

    //Probabilidad entre ataques
    public int attack1Probability = 70;

    public bool isAttacking;

    //estados
    public float sightRange = 10f;
    private bool playerInSightRange, playerInAttackRange;
    public float timeBeforeDying = 3f;
    private bool isDead;

    Animator animator;

    private void Awake()
    {
        //buscar al jugador si no esta asignado
        if (player == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("No se encuentra al jugador");
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError("No hay NavMeshAgent asignado al boss final");

        animator = GetComponent<Animator>();

        healthBar = GetComponentInChildren<FinalBossHealthBar>();

        // Configurar AudioSource automáticamente
        SetupAudioSource();
    }

   
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
        }

        // Configurar propiedades para audio 3D espacial
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = audioMinDistance;
        audioSource.maxDistance = audioMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f;

        // Configuración adicional recomendada
        audioSource.loop = false;
        audioSource.priority = 64; // Prioridad más alta para el boss
    }

    private void Start()
    {
        currentHealth = maxHealth;
        healthBar.UpdateHealthBar(currentHealth, maxHealth);

        // Inicializar timer de sonido de patrulla
        nextPatrolSoundTime = Time.time + Random.Range(minPatrolSoundInterval, maxPatrolSoundInterval);
    }

    private void Update()
    {
        if (isDead) return;

        //comprobar si el jugador esta en rango
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);

        // Usar el rango mayor de los dos ataques
        float maxAttackRange = Mathf.Max(attack1Range, attack2Range);
        playerInAttackRange = Physics.CheckSphere(transform.position, maxAttackRange, whatIsPlayer);

        //Logica estados (similar a Enemy - con patrulla)
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

    // PATRULLA 
    private void Patroling()
    {
        agent.isStopped = false;
        isPatrolling = true;

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
        UpdateAnimator(normalizedSpeed);

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

    private void UpdateAnimator(float Speed)
    {
        animator.SetFloat("Speed", Speed);
    }

    //Seguir al jugador
    private void ChasePlayer()
    {
        agent.isStopped = false;
        isPatrolling = false;
        agent.SetDestination(player.position);
        float currentSpeed = agent.velocity.magnitude;
        UpdateAnimator(currentSpeed);
    }

    private void AttackPlayer()
    {
        // El boss se detiene para atacar
        agent.isStopped = true;
        isPatrolling = false;
        UpdateAnimator(0f);

        // Mira al jugador
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (!isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        canMove = false;

        // Elegir ataque aleatoriamente
        int randomValue = Random.Range(0, 100);
        bool useAttack1 = randomValue < attack1Probability;

        if (useAttack1)
        {
            yield return StartCoroutine(ExecuteAttack1());
        }
        else
        {
            yield return StartCoroutine(ExecuteAttack2());
        }

        // Reactivar movimiento
        agent.isStopped = false;

        yield return new WaitForSeconds(timeBetweenAttacks);
        canMove = true;
        isAttacking = false;
    }

    // Ataque 1 - Ataque normal
    private IEnumerator ExecuteAttack1()
    {
        Debug.Log("¡Jefe usa ataque normal!");

        if (animator != null)
        {
            animator.SetBool("Attack1", true);
        }

        // Reproducir sonido de ataque 1
        PlayAttack1Sound();

        yield return new WaitForSeconds(attack1Delay);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attack1Range)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attack1Damage);
                Debug.Log("Ataque 1 conectado - Daño: " + attack1Damage);
            }
        }

        if (animator != null)
        {
            animator.SetBool("Attack1", false);
        }
    }

    // Ataque 2 - Ataque especial poderoso
    private IEnumerator ExecuteAttack2()
    {
        Debug.Log("¡Jefe usa ataque especial!");

        if (animator != null)
        {
            animator.SetBool("Attack2", true);
        }

        // Reproducir sonido de ataque 2
        PlayAttack2Sound();

        yield return new WaitForSeconds(attack2Delay);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attack2Range)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attack2Damage);
                Debug.Log("Ataque 2 conectado - Daño: " + attack2Damage);
            }
        }

        if (animator != null)
        {
            animator.SetBool("Attack2", false);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        healthBar.UpdateHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    //muerte
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        agent.enabled = false;
        GetComponent<Collider>().enabled = false;

        // Reproducir sonido de muerte
        PlayDeathSound();

        StartCoroutine(DyingCoroutine());
    }

    private IEnumerator DyingCoroutine()
    {
        if (animator != null)
        {
            animator.SetBool("isDead", true);
        }

        // Esperar el tiempo de muerte o la duración del sonido, lo que sea mayor
        float waitTime = timeBeforeDying;
        if (deathSound != null)
        {
            waitTime = Mathf.Max(timeBeforeDying, deathSound.length);
        }

        yield return new WaitForSeconds(waitTime);

        CleanupPersistenObjects();

        SceneManager.LoadScene("CinemFinal");
    }

    private void CleanupPersistenObjects()
    {
        if (CameraPersist.instance != null)
        {
            CameraPersist.instance.ResetCamera();
        }

        if (player != null)
        {
            Destroy(player.gameObject);
        }

        if (PlayerHealth.instance != null)
        {
            Destroy(PauseManager.Instance.gameObject);
            PauseManager.Instance = null;
        }
        //si se borra algo, que sea desde aqui
        if (PauseManager.Instance != null)
        {
            Destroy(PauseManager.Instance.gameObject);
            PauseManager.Instance = null;
        }

        MusicManager.ResetMusic();

        Time.timeScale = 1f;

        Destroy(gameObject);
    }

    // MÉTODOS DE AUDIO

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

    private void PlayAttack1Sound()
    {
        if (attack1Sounds == null || attack1Sounds.Length == 0)
            return;

        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource no encontrado en {gameObject.name}");
            return;
        }

        // Seleccionar un sonido aleatorio
        AudioClip randomClip = attack1Sounds[Random.Range(0, attack1Sounds.Length)];

        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip, attack1SoundVolume);
        }
    }
    private void PlayAttack2Sound()
    {
        if (attack2Sounds == null || attack2Sounds.Length == 0)
            return;

        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource no encontrado en {gameObject.name}");
            return;
        }

        // Seleccionar un sonido aleatorio
        AudioClip randomClip = attack2Sounds[Random.Range(0, attack2Sounds.Length)];

        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip, attack2SoundVolume);
        }
    }

 
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
        Gizmos.DrawWireSphere(transform.position, attack1Range);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attack2Range);
    }
}