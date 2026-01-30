using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class NextLevel : MonoBehaviour
{
    public string nextLevelName; // Nombre de la siguiente escena

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(LoadNextLevel());
        }
    }

    private IEnumerator LoadNextLevel()
    {
        // Aquí puedes agregar efectos de transición si lo deseas
        yield return new WaitForSeconds(1f); // Espera 1 segundo antes de cargar la siguiente escena
        SceneManager.LoadScene(nextLevelName);
    }
    
}
