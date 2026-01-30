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

    [Header("IA")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;

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
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;


        //comprobar si el jugador esta en rango
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        
        // Usar el rango mayor de los dos ataques
        float maxAttackRange = Mathf.Max(attack1Range, attack2Range);
        playerInAttackRange = Physics.CheckSphere(transform.position, maxAttackRange, whatIsPlayer);

        //Logica estados
        if (playerInSightRange && !playerInAttackRange)
        {
            ChasePlayer();
        }
        else if (playerInAttackRange)
        {
            AttackPlayer();
        }

        else
        {
            UpdateAnimator(0f);
        }
    }

    private void UpdateAnimator(float Speed)
    {
        animator.SetFloat("Speed", Speed);
    }
    
    
    //Seguir al jugador
    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        float currentSpeed = agent.velocity.magnitude;
        UpdateAnimator(currentSpeed);
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;
        UpdateAnimator(0f);

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

        yield return new WaitForSeconds(timeBetweenAttacks);
        canMove = true;
        isAttacking = false;
    }

    // Ataque 1 - Ataque normal
    private IEnumerator ExecuteAttack1()
    {
        Debug.Log("¡Jefe usa ataque normal!");

        if(animator != null)
        {
            animator.SetBool("Attack1", true);
        }

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

        if (animator !=null)
        {
            animator.SetBool("attack2", true);
        }

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

        if (animator !=null)
        {
            animator.SetBool("Attack2", false);
        }
    }


    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + "recibio" + amount + "de daño. Vida restante" + currentHealth);

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

        StartCoroutine(DyingCoroutine());
    }

    private IEnumerator DyingCoroutine()
    {
        if(animator !=null)
        {
            animator.SetBool("isDead", true);
        }

        yield return new WaitForSeconds(timeBeforeDying);

        if (CameraPersist.instance !=null)
        {
            CameraPersist.instance.ResetCamera();
        }

        Destroy(gameObject);

        if (player !=null)
        {
            Destroy(player.gameObject);
            Debug.Log("Jugador destruido");
        }

        SceneManager.LoadScene("CinemFinal");
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
