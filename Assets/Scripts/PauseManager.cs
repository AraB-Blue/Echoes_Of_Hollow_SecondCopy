using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;
    
    public GameObject pauseMenuUI;
    private bool isPaused = false;

    
    void Awake()
    {
        if (Instance != null && Instance !=this)
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
        // menu cerrado al cargar una nueva escena
        if (!IsGameScene(scene.name))
        {
            if (Instance == this)
            {
                Instance = null;
            }
            Destroy(gameObject);
            return;
            
        }

        if(isPaused)
        {
            ClosePauseMenu();
        }
    }

    private bool IsGameScene(string nombreEscena)
    {
        return nombreEscena.Contains("Nivel");
    }

    void Update()
    {
        // Pulsar ESC abre o cierra
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ClosePauseMenu();   // ESC cierra si esta abierto
            
            else
                OpenPauseMenu();    // ESC abre si esta cerrado
        }
    }

    public void OpenPauseMenu()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ClosePauseMenu()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Se ha cerrado el juego");
    }
}
