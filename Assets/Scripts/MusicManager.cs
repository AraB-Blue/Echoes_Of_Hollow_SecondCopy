using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource audioSource;

    public AudioClip musicaNivel;
    public AudioClip musicaJefe;

    // Escenas que usan m�sica de nivel
    public List<string> escenasNivel;

    // Escenas que usan m�sica de jefe
    public List<string> escenasJefe;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
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
    }

    public void CambiarMusica(AudioClip nuevaMusica)
    {
        // Si ya est� sonando esa m�sica, NO reiniciar
        if (audioSource.clip == nuevaMusica && audioSource.isPlaying)
            return;

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
