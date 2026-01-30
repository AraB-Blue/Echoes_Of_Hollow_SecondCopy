using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioSource audioSource;

    public AudioClip musicaNivel;
    public AudioClip musicaJefe;

    // Escenas que usan música de nivel
    public List<string> escenasNivel;

    // Escenas que usan música de jefe
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
        // Si ya está sonando esa música, NO reiniciar
        if (audioSource.clip == nuevaMusica && audioSource.isPlaying)
            return;

        audioSource.clip = nuevaMusica;
        audioSource.Play();
    }
}
