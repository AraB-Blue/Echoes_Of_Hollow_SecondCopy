using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource audioSource;

    public AudioClip musicaNivel;
    public AudioClip musicaJefe;
    public AudioClip musicaMenu; // Música para el menú (opcional, puede ser null)

    // Escenas que usan música de nivel
    public List<string> escenasNivel;

    // Escenas que usan música de jefe
    public List<string> escenasJefe;

    // Escenas que usan música de menú
    public List<string> escenasMenu;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // El singleton ya existe, no hacer nada más, simplemente destruirse
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string nombreEscena = scene.name;

        if (escenasJefe.Contains(nombreEscena))
        {
            CambiarMusica(musicaJefe);
        }
        else if (escenasNivel.Contains(nombreEscena))
        {
            CambiarMusica(musicaNivel);
        }
        else if (escenasMenu != null && escenasMenu.Contains(nombreEscena))
        {
            CambiarMusica(musicaMenu);
        }
    }

    public void CambiarMusica(AudioClip nuevaMusica)
    {
        if (nuevaMusica == null) return;

        // Si ya está sonando esa música, NO reiniciar
        if (audioSource.clip == nuevaMusica && audioSource.isPlaying)
            return;

        audioSource.Stop();
        audioSource.clip = nuevaMusica;
        audioSource.Play();
    }

    public static void ResetMusic()
    {
        if (instance != null)
        {
            instance.audioSource.Stop();
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}