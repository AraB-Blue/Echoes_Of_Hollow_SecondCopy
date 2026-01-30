using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [Header("Configuración de la poción")]
    [SerializeField] private float healAmount = 25f;
    [SerializeField] private bool destroyOnUse = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (PlayerHealth.instance != null)
        {
            PlayerHealth.instance.Heal(healAmount);
        }

        if (destroyOnUse)
        {
            gameObject.SetActive(false);
        }
    }
}
