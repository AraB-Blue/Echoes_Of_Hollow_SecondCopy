using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class AnimInicio : MonoBehaviour
{
    [Header("Configuración de Video")]
    public VideoPlayer videoPlayer;
    
    [Header("Configuración de Tiempos")]
    [Tooltip("Tiempo hasta que aparece la elección (en segundos)")]
    public float tiempoHastaEleccion = 2f; //ajustar mas adelante
    
    [Header("UI de Elección")]
    public GameObject panelEleccion;
    
    private bool eleccionMostrada = false;
    private bool cinematicaCompletada = false;
    private bool debeCargarNivel = false;
    
    void Start()
    {
        if(panelEleccion != null)
            panelEleccion.SetActive(false);
        
        if(videoPlayer != null)
        {
            videoPlayer.Play();
            // Suscribirse al evento cuando el video termina
            videoPlayer.loopPointReached += OnVideoFinished;
        }
            
        StartCoroutine(AnimInicial());
    }
    
    void Update()
    {
        // Saltar antes de la elección
        if(Input.GetKeyDown(KeyCode.Space) && !eleccionMostrada && !cinematicaCompletada)
        {
            StopAllCoroutines();
            SaltarAEleccion();
        }

        // Permitir saltar DESPUÉS de elegir "Seguir"(cuando el video se reanuda)
        if (Input.GetKeyDown(KeyCode.Space) && cinematicaCompletada && debeCargarNivel)
        {
            CargarNivel();
        }

        // Verificar continuamente si el video ha terminado cuando debe cargar el nivel
        if (debeCargarNivel && videoPlayer != null)
        {
            // Verificar si el video está cerca del final o ya terminó
            if(!videoPlayer.isPlaying || videoPlayer.time >= videoPlayer.length - 0.1f)
            {
                CargarNivel();
            }
        }
    }
    
    private IEnumerator AnimInicial()
    {
        yield return new WaitForSeconds(tiempoHastaEleccion);
        
        if(!cinematicaCompletada)
        {
            MostrarEleccion();
        }
    }
    
    private void SaltarAEleccion()
    {
        // Adelantar el video al momento de la elección
        if(videoPlayer != null)
        {
            videoPlayer.time = tiempoHastaEleccion;
        }
        MostrarEleccion();
    }
    
    private void MostrarEleccion()
    {
        eleccionMostrada = true;
        
        // Pausar el video
        if(videoPlayer != null)
            videoPlayer.Pause();
        
        if(panelEleccion != null)
            panelEleccion.SetActive(true);
    }
    
    public void ElegirSeguir()
    {
        cinematicaCompletada = true;
        debeCargarNivel = true;
        
        // Asegurarse de ocultar el panel
        if(panelEleccion != null)
        {
            panelEleccion.SetActive(false);
            Debug.Log("Panel ocultado");
        }
        
        // Reanudar el video
        if(videoPlayer != null)
        {
            videoPlayer.Play();
            Debug.Log($"Video reanudado. Tiempo actual: {videoPlayer.time}, Duración: {videoPlayer.length}");
        }
    }

 
    public void ElegirFinalAlternativo()
    {
        cinematicaCompletada = true;
        
        if(panelEleccion != null)
            panelEleccion.SetActive(false);
        
        // Detener el video
        if(videoPlayer != null)
            videoPlayer.Stop();
        
        SceneManager.LoadScene("FinalAlternativo");
    }
    
    private void OnVideoFinished(VideoPlayer vp)
    {
        // Cuando el video termina, ir al nivel
        if(debeCargarNivel)
        {
            CargarNivel();
        }
    }
    
    private void CargarNivel()
    {
        // Evitar cargar múltiples veces
        if(!debeCargarNivel) return;
        
        debeCargarNivel = false;
        Debug.Log("Cargando Nivel1Inicio");
        SceneManager.LoadScene("Nivel1Inicio");
    }
    
    void OnDestroy()
    {
        // Limpiar el evento
        if(videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
