using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    private bool activado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;

        if (other.CompareTag("Player"))
        {
            activado = true;

            // Cambia a música de jefe sin cortar si ya está sonando
            MusicManager.instance.CambiarMusica(
                MusicManager.instance.musicaJefe
            );
        }
    }
}
