using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.Android.AndroidGame;

public class EnemyDamageFlash : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Deja vacío para buscar automáticamente TODOS los Renderers en los hijos")]
    public Renderer[] renderers;

    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    [Header("Modo de Flash")]
    [Tooltip("Cambiar color completo o usar emisión")]
    public bool useEmission = false;
    public float emissionIntensity = 2f;

    private List<Material> allMaterialInstances = new List<Material>();
    private List<Color> originalColors = new List<Color>();

    private void Start()
    {
        // Buscar TODOS los Renderers automáticamente si no están asignados
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogError($"No se encontró ningún Renderer en {gameObject.name} ni en sus hijos.");
                return;
            }
            else
            {
                Debug.Log($"Se encontraron {renderers.Length} Renderers automáticamente:");
                foreach (Renderer r in renderers)
                {
                    Debug.Log($"  - {r.gameObject.name}");
                }
            }
        }

        // Crear instancias de TODOS los materiales de TODOS los renderers
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.materials;
            foreach (Material mat in mats)
            {
                allMaterialInstances.Add(mat);
                originalColors.Add(mat.color);
            }
        }

        Debug.Log($"Total de materiales a flashear: {allMaterialInstances.Count}");
    }

    private IEnumerator DoFlash()
    {
        if (useEmission)
        {
            // Flash con emisión en TODOS los materiales
            foreach (Material mat in allMaterialInstances)
            {
                if (mat == null) continue;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", flashColor * emissionIntensity);
            }

            yield return new WaitForSeconds(flashDuration);

            foreach (Material mat in allMaterialInstances)
            {
                if (mat == null) continue;
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
        else
        {
            // Flash cambiando color en TODOS los materiales
            foreach (Material mat in allMaterialInstances)
            {
                if (mat == null) continue;
                mat.color = flashColor;
            }

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < allMaterialInstances.Count; i++)
            {
                if (allMaterialInstances[i] == null) continue;
                allMaterialInstances[i].color = originalColors[i];
            }
        }
    }

    public void Flash()
    {
        if (allMaterialInstances != null && allMaterialInstances.Count > 0)
        {
            StopAllCoroutines();
            StartCoroutine(DoFlash());
        }
    }

    // Limpiar las instancias de materiales
    private void OnDestroy()
    {
        if (allMaterialInstances != null)
        {
            foreach (Material mat in allMaterialInstances)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }

        allMaterialInstances.Clear();
        originalColors.Clear();
    }
}