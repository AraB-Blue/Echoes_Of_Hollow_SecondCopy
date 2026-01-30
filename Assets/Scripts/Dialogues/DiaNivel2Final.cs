using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DiaNivel2Final : MonoBehaviour
{
    //UI References
    [SerializeField]
    private GameObject dialogueCanvas;

    [SerializeField]
    private TMP_Text speakerText;

    [SerializeField]
    private TMP_Text dialogueText;

    //[SerializeField]
    //private Image portraitImage;

    //dialogue content
    [SerializeField]
    private string[] speaker;

    [SerializeField]
    [TextArea]
    private string[] dialogueWords;

    //[SerializeField]
    //private Sprite[] portrait;

    //Input
    [SerializeField]
    private InputActionReference interactAction;

    private bool dialogueStarted;
    private bool dialogueActivated;
    private bool dialogueCompleted;
    private int step;

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.Disable();
        }
    }

    void Update()
    {
        if (interactAction != null && interactAction.action.WasPressedThisFrame() && dialogueActivated && dialogueStarted)
        {
            // Primera vez que presiona: detener al jugador y mostrar diálogo
            if (step >= speaker.Length)
            {
                dialogueCanvas.SetActive(false);
                step = 0;
                dialogueCompleted = true;
                dialogueStarted = false;
                ResumeGame();
            }
            // Siguientes presiones: avanzar el diálogo
            else
            {
                // Validación para evitar errores de índice
                if (step < speaker.Length && step < dialogueWords.Length)
                {
                    speakerText.text = speaker[step];
                    dialogueText.text = dialogueWords[step];
                }
                step += 1;
            }

        }
    }

    private void OnValidate()
    {
        // Verifica que los arrays tengan la misma longitud
        if (speaker.Length != dialogueWords.Length)
        {
            Debug.LogWarning("Los arrays de speaker y dialogueWords deben tener la misma longitud");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !dialogueCompleted)
        {
            dialogueActivated = true;
            dialogueStarted = true;

            PauseGame();
            dialogueCanvas.SetActive(true);

            // Mostrar el primer diálogo
            if (step < speaker.Length && step < dialogueWords.Length)
            {
                speakerText.text = speaker[step];
                dialogueText.text = dialogueWords[step];
            }
            step += 1;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogueActivated = false;
            dialogueCanvas.SetActive(false);
            dialogueStarted = false;
            step = 0;
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
    }

}
