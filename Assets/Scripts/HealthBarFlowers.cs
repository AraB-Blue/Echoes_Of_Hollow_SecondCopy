using UnityEngine;
using UnityEngine.UI;

public class HealthBarFlowers : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] flowers; // 5 imagenes

    void Start()
    {
        // Buscar el PlayerHealth si no esta asignado
        if (playerHealth == null)
        {
            playerHealth = PlayerHealth.instance;
        }

        // Actualizar la barra inicial
        UpdateBar();
    }

    void Update()
    {
        // Actualizar continuamente (o puedes usar eventos)
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[HealthBarFlowers] PlayerHealth no asignado!");
            return;
        }

        float health = playerHealth.GetCurrentHealth();
        float maxHealth = playerHealth.GetMaxHealth();
        float healthPerFlower = maxHealth / flowers.Length;

        for (int i = 0; i < flowers.Length; i++)
        {
            float flowerHealth = Mathf.Clamp(
                health - (i * healthPerFlower),
                0,
                healthPerFlower
            );
            flowers[i].fillAmount = flowerHealth / healthPerFlower;
        }
    }
}

