using UnityEngine;

[System.Serializable]
public class ComboAttack
{
    public string nombre = "Ataque";
    public int damage = 20;
    public float range = 2f;
    public float angle = 60f;
    public float duration = 0.4f; // Tiempo entre ataques
    
    [Tooltip("Nombre del trigger en el Animator para este ataque")]
    public string animationTrigger = "Attack1";
}