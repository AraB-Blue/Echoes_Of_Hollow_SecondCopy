using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
   [SerializeField] private Slider slider;
   [SerializeField] private Transform target;
   [SerializeField] private Vector3 offset;

   private Camera camera;
   
   void Start()
   {
     FindCamera();
   }

   public void UpdateHealthBar(float currentValue, float maxValue)
   {
    slider.value = currentValue / maxValue;

   }

    
    void Update()
    {
        if (camera == null)
        {
            FindCamera();
            return;
        }


        transform.rotation = camera.transform.rotation;
        transform.position = target.position + offset;
    }

    private void FindCamera()
    {
        camera = Camera.main;
    }
}
