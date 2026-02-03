using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class PlayerScript : MonoBehaviour
{
    public static PlayerScript instance;
    private Rigidbody rb;
    private NavMeshAgent agent;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // IMPORTANTE: sin esto, la suscripción se acumula cada vez que se crea un nuevo jugador
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (agent == null) return;

        agent.enabled = false;
        agent.enabled = true;

        // Notificar a la cámara de que hay un jugador nuevo disponible
        if (CameraPersist.instance != null)
        {
            CameraPersist.instance.OnPlayerSpawned();
        }
    }
}
