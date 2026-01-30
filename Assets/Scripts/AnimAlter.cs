using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class AnimAlter : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string menuSceneName = "menu";
    [SerializeField] private bool allowSkipWithSpace = true;
    
    private bool isVideoFinished = false;

    void Start()
    {
        // Encontrar videpLayer si no esta asignado
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            
            // Si aún no existe, crear uno
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
        }

        // Configurar el VideoPlayer
        ConfigureVideoPlayer();
    }

    void Update()
    {
        // Saltar video
        if (allowSkipWithSpace && Input.GetKeyDown(KeyCode.Space))
        {
            LoadMenuScene();
        }
    }

    private void ConfigureVideoPlayer()
    {
        
        videoPlayer.playOnAwake = true;
        videoPlayer.waitForFirstFrame = true;
        
        
        videoPlayer.loopPointReached += OnVideoFinished;
        
        // Iniciar la reproducción si no está configurado para reproducir automáticamente
        if (!videoPlayer.playOnAwake)
        {
            videoPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (!isVideoFinished)
        {
            isVideoFinished = true;
            LoadMenuScene();
        }
    }

    private void LoadMenuScene()
    {
        // Desuscribirse del evento antes de cambiar de escena
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        
        SceneManager.LoadScene(menuSceneName);
    }

    void OnDestroy()
    {
        
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}