using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Combos de ataque")]
    public List<ComboAttack> comboList = new List<ComboAttack>()
    {
        new ComboAttack() { nombre = "Corte rápido", damage = 15, range = 2f, angle = 60f, duration = 0.4f, animationTrigger = "Attack1" },
        new ComboAttack() { nombre = "Estocada", damage = 25, range = 2.5f, angle = 45f, duration = 0.6f, animationTrigger = "Attack2" },
        new ComboAttack() { nombre = "Golpe final", damage = 40, range = 3f, angle = 90f, duration = 0.8f, animationTrigger = "Attack3" }
    };

    [Header("Ajustes generales")]
    public float comboResetTime = 1f;
    public LayerMask enemyLayer; // si lo dejas en 0, el script buscará en todas las capas
    [Tooltip("Origen del ataque (por ejemplo punta de espada). Si es null se usa el transform del jugador.")]
    public Transform attackOrigin;

    [Header("Animación de ataque")]
    [Tooltip("Momento en el que se ejecuta el daño (0-1, donde 0.5 = mitad de la animación)")]
    [Range(0f, 1f)]
    public float damagePoint = 0.5f;

    [Header("Sistema de Audio")]
    [Tooltip("Déjalo vacío, se obtiene del PlayerMovement automáticamente")]
    public AudioSource audioSource;

    [Header("Sonidos de Ataque")]
    [Tooltip("Sonidos de swing/corte de espada (se reproduce al inicio del ataque)")]
    public AudioClip[] attackSwingSounds;

    [Tooltip("Sonidos de impacto cuando golpeas a un enemigo")]
    public AudioClip[] attackHitSounds;

    [Tooltip("Sonido especial para el ataque final del combo")]
    public AudioClip ultimateAttackSound;

    [Header("Configuración de Volumen")]
    [Tooltip("Volumen del sonido de swing (0-1)")]
    [Range(0f, 1f)]
    public float swingVolume = 0.5f;

    [Tooltip("Volumen del sonido de impacto (0-1)")]
    [Range(0f, 1f)]
    public float hitVolume = 0.7f;

    [Tooltip("Volumen del ataque final (0-1)")]
    [Range(0f, 1f)]
    public float ultimateVolume = 0.9f;

    private int comboIndex = 0;
    private bool isAttacking = false;
    private float lastAttackTime;
    private PlayerMovement movement;
    private Animator animator;


    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        animator = movement.GetAnimator();

        if (attackOrigin == null) attackOrigin = transform;

        // Obtener AudioSource del PlayerMovement si no está asignado
        SetupAudioSource();
    }

    /// <summary>
    /// Configura el AudioSource automáticamente desde PlayerMovement
    /// </summary>
    private void SetupAudioSource()
    {
        if (audioSource == null && movement != null)
        {
            // Intentar obtener el AudioSource del componente PlayerMovement
            audioSource = movement.audioSource;

            if (audioSource == null)
            {
                // Si tampoco está en PlayerMovement, buscar en el GameObject
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                Debug.Log($"AudioSource encontrado para ataques en {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"No se encontró AudioSource en {gameObject.name}. Los sonidos de ataque no se reproducirán.");
            }
        }
    }

    void Update()
    {
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboIndex = 0;
            if (animator != null)
                animator.SetInteger("ComboIndex", 0);
        }

        if (Input.GetMouseButtonDown(0) && !isAttacking)
            StartCoroutine(PerformAttack());
    }

    IEnumerator PerformAttack()
    {
        if (comboList == null || comboList.Count == 0)
        {
            Debug.LogWarning("No hay ataques en la lista de combos.");
            yield break;
        }

        isAttacking = true;
        ComboAttack currentAttack = comboList[comboIndex];
        Debug.Log($"Ataque {comboIndex + 1}: {currentAttack.nombre} | Daño {currentAttack.damage}");

        // Reproducir sonido de swing al inicio del ataque
        PlayAttackSwingSound();

        // Activar animación de ataque
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            animator.SetInteger("ComboIndex", comboIndex);

            // Trigger específico del ataque
            if (!string.IsNullOrEmpty(currentAttack.animationTrigger))
                animator.SetTrigger(currentAttack.animationTrigger);
        }

        // Esperar hasta el punto de daño en la animación
        float damageTime = currentAttack.duration * damagePoint;
        yield return new WaitForSeconds(damageTime);

        // Ejecutar el daño
        ExecuteAttack(currentAttack);

        // Esperar el resto de la duración
        yield return new WaitForSeconds(currentAttack.duration - damageTime);

        // Resetear estado de ataque
        if (animator != null)
            animator.SetBool("IsAttacking", false);

        // Permitir movimiento nuevamente
        if (movement != null)
            movement.canMove = true;

        comboIndex++;
        if (comboIndex >= comboList.Count) comboIndex = 0;

        lastAttackTime = Time.time;
        isAttacking = false;
    }

    void ExecuteAttack(ComboAttack attack)
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;

        // Si enemyLayer está vacío (valor 0) usamos OverlapSphere sin filtro para no excluir por error
        Collider[] hitColliders = (enemyLayer.value != 0)
            ? Physics.OverlapSphere(origin, attack.range, enemyLayer)
            : Physics.OverlapSphere(origin, attack.range);

        Debug.Log($"[PlayerAttack] OverlapSphere encontró {hitColliders.Length} colliders para '{attack.nombre}' (range={attack.range}).");

        if (hitColliders.Length == 0)
        {
            Debug.LogWarning("[PlayerAttack] No se detectaron colliders. Revisa enemyLayer, colliders y que estén activos.");
        }

        bool hitSomething = false;

        foreach (Collider col in hitColliders)
        {
            // Ignorar cualquier collider que pertenezca al mismo root que el jugador (por ejemplo colliders del propio jugador)
            if (col.transform.root == transform.root)
            {
                //Debug.Log($"[PlayerAttack] Ignorando collider propio: {col.name}");
                continue;
            }
            // Usar el punto más cercano del collider para calcular el ángulo/range real
            Vector3 closestPoint = col.ClosestPoint(origin);
            Vector3 dirToTarget = (closestPoint - origin).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

            if (angleToTarget <= attack.angle / 2f)
            {
                IDamageable damageable = GetDamageableFromCollider(col);
                if (damageable != null)
                {
                    damageable.TakeDamage(attack.damage);
                    Debug.Log($"Golpeó a {col.name} con {attack.nombre} (daño={attack.damage}, angle={angleToTarget:F1})");
                    hitSomething = true;
                }
                else
                {
                    Debug.LogWarning($"[PlayerAttack] Collider '{col.name}' (capa={LayerMask.LayerToName(col.gameObject.layer)}) no tiene componente IDamageable (ni en parents/children).");
                }
            }
            else
            {
                Debug.Log($"[PlayerAttack] '{col.name}' fuera de ángulo (angle={angleToTarget:F1}, requerido<={attack.angle / 2f})");
            }
        }

        // Reproducir sonido de impacto si golpeaste algo
        if (hitSomething)
        {
            PlayAttackHitSound();
        }
    }

    private IDamageable GetDamageableFromCollider(Collider col)
    {
        if (col == null) return null;
        IDamageable d = col.GetComponent<IDamageable>();
        if (d != null) return d;
        d = col.GetComponentInParent<IDamageable>();
        if (d != null) return d;
        d = col.GetComponentInChildren<IDamageable>();
        return d;
    }

    // MÉTODOS DE AUDIO

    /// <summary>
    /// Reproduce el sonido de swing del ataque
    /// </summary>
    private void PlayAttackSwingSound()
    {
        if (audioSource == null) return;

        // Si es el último ataque del combo y tiene sonido especial
        bool isUltimateAttack = (comboIndex == comboList.Count - 1);

        if (isUltimateAttack && ultimateAttackSound != null)
        {
            audioSource.PlayOneShot(ultimateAttackSound, ultimateVolume);
            return;
        }

        // Sonido normal de swing
        if (attackSwingSounds != null && attackSwingSounds.Length > 0)
        {
            AudioClip randomSwing = attackSwingSounds[Random.Range(0, attackSwingSounds.Length)];
            if (randomSwing != null)
            {
                audioSource.PlayOneShot(randomSwing, swingVolume);
            }
        }
    }

    /// <summary>
    /// Reproduce el sonido de impacto cuando golpeas a un enemigo
    /// </summary>
    private void PlayAttackHitSound()
    {
        if (audioSource == null) return;

        if (attackHitSounds != null && attackHitSounds.Length > 0)
        {
            AudioClip randomHit = attackHitSounds[Random.Range(0, attackHitSounds.Length)];
            if (randomHit != null)
            {
                audioSource.PlayOneShot(randomHit, hitVolume);
            }
        }
    }

    /// <summary>
    /// Reproduce un sonido personalizado de ataque (para uso externo)
    /// </summary>
    public void PlayCustomAttackSound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (comboList == null || comboList.Count == 0) return;
        ComboAttack currentAttack = comboList[Mathf.Clamp(comboIndex, 0, comboList.Count - 1)];
        Gizmos.color = Color.red;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector3 forward = transform.forward;
        Quaternion left = Quaternion.Euler(0, -currentAttack.angle / 2f, 0);
        Quaternion right = Quaternion.Euler(0, currentAttack.angle / 2f, 0);
        Vector3 leftDir = left * forward;
        Vector3 rightDir = right * forward;
        Gizmos.DrawRay(origin, leftDir * currentAttack.range);
        Gizmos.DrawRay(origin, rightDir * currentAttack.range);
        Gizmos.DrawWireSphere(origin, currentAttack.range);
    }

    // DEBUG: Detectar colisiones durante ataques (opcional, para diagnóstico)
    void OnCollisionEnter(Collision collision)
    {
        if (isAttacking)
        {
            Debug.LogWarning($"[PlayerAttack] Colisión durante ataque con: {collision.gameObject.name}");
        }
    }
}