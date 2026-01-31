using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class AnimFinal : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public string menuSceneName = "Menu";
    public bool skipWithEscape = true;
    
    void Start()
    {
        // Asegurar que todo está limpio
        Time.timeScale = 1f;
        
        if (videoPlayer != null)
        {
            // Cuando termine el video, volver al menú
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    void Update()
    {
        // Permitir saltar el video con ESC
        if (skipWithEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        ReturnToMenu();
    }

    private void ReturnToMenu()
    {
        // Limpiar cualquier objeto persistente que pudiera quedar
        CleanupRemainingObjects();
        
        SceneManager.LoadScene(menuSceneName);
    }

    private void CleanupRemainingObjects()
    {
        // Por si acaso quedó algo
        if (PauseManager.Instance != null)
        {
            Destroy(PauseManager.Instance.gameObject);
            PauseManager.Instance = null;
        }
        
        if (PlayerHealth.instance != null)
        {
            PlayerHealth.instance = null;
        }
        
        if (CameraPersist.instance != null)
        {
            Destroy(CameraPersist.instance.gameObject);
            CameraPersist.instance = null;
        }

        MusicManager.ResetMusic();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
