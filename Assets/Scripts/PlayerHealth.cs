using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Configuración de muerte")]
    [SerializeField] private float timeBeforeMenu = 3f;
    [SerializeField] private string menuSceneName = "menu";

    private float lastDamageTime = -999f;
    public static PlayerHealth instance;
    private PlayerHealthBar healthBarCache;
    private HealthBarFlowers flowersBarCache; // Nueva referencia
    private bool isDead = false;

    // Métodos públicos para acceder a la vida desde otros scripts
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;

    void Awake()
    {
        // Singleton: mantener la vida persistente entre escenas
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Si es la primera vez, inicializar la vida
        if (currentHealth == 0)
        {
            currentHealth = maxHealth;
        }
    }

    void Start()
    {
        Debug.Log($"[PlayerHealth] Vida actual: {currentHealth}/{maxHealth}");
        CacheHealthBars();
    }

    // Cachear ambas barras de vida
    private void CacheHealthBars()
    {
        healthBarCache = FindFirstObjectByType<PlayerHealthBar>();
        flowersBarCache = FindFirstObjectByType<HealthBarFlowers>();

        // Actualizar ambas barras al inicio
        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        lastDamageTime = Time.time;
        Debug.Log($"[PlayerHealth] ¡Recibió {damage} de daño! Vida restante: {currentHealth}/{maxHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI()
    {
        // Actualizar la barra antigua
        if (healthBarCache != null)
        {
            healthBarCache.SetValue(currentHealth);
        }

        // Actualizar la barra de flores
        if (flowersBarCache != null)
        {
            flowersBarCache.UpdateBar();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] Curado {amount}. Vida actual: {currentHealth}/{maxHealth}");

        UpdateHealthUI();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[PlayerHealth] ¡El jugador ha muerto!");
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(timeBeforeMenu);
        Debug.Log("[PlayerHealth] volviendo al menú...");

        if (instance == this)
        {
            instance = null;
        }
        Destroy(gameObject);
        SceneManager.LoadScene(menuSceneName);
    }

    // Llamar esto cuando cambies de escena para reconectar las barras
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheHealthBars();
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (currentHealth / maxHealth > 0.5f)
                Gizmos.color = Color.green;
            else if (currentHealth / maxHealth > 0.25f)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
}