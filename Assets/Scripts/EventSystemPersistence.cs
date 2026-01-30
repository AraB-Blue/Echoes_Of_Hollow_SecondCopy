using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class EventSystemPersistence : MonoBehaviour
{
    public static EventSystemPersistence Instance;
    private EventSystem eventSystem;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        eventSystem = GetComponent<EventSystem>();
    }

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
        // Eliminar duplicados
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        foreach (EventSystem es in eventSystems)
        {
            if (es.gameObject != this.gameObject)
            {
                Destroy(es.gameObject);
            }
        }

        // Si volvemos al menú → reset
        if (scene.name == "Menu")   // usa el nombre real de tu escena
        {
            ResetEventSystem();
        }
    }

    void ResetEventSystem()
    {
        eventSystem.SetSelectedGameObject(null);
        eventSystem.enabled = false;
        eventSystem.enabled = true;
    }
}
