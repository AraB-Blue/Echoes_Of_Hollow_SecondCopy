using Unity.Cinemachine;
using UnityEngine;

public class ActivateVirtualCamera : MonoBehaviour
{
    private CinemachineCamera vcam;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();

        // Forzar activación después de un frame
        StartCoroutine(ActivateCamera());
    }

    System.Collections.IEnumerator ActivateCamera()
    {
        yield return new WaitForEndOfFrame();

        if (vcam != null)
        {
            vcam.enabled = false;
            vcam.enabled = true;
        }
    }
}
