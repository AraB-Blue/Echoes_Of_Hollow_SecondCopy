using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasHealthPersistance : MonoBehaviour
{
    public static CanvasHealthPersistance Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        if (!EsEscenaDeJuego(scene.name))
        {
            if (Instance == this)
            {
                Instance = null;
            }
            Destroy(gameObject);
        }
    }

    private bool EsEscenaDeJuego(string nombreEscena)
    {
        return nombreEscena.Contains("Nivel");
    }

}
