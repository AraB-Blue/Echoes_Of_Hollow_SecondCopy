using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;

    [SerializeField] RectTransform _barRect;

    [SerializeField] RectMask2D _mask;

    private float _maxRightMask;
    private float _initialRightMask;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // x = left, w = top, y = bottom, z = right
        _maxRightMask = _barRect.rect.width;
        _initialRightMask = _mask.padding.z;

    }

    public void SetValue(float newValue)
    {
        // Calcular el porcentaje de vida actual
        float healthPercentage = newValue / _playerHealth.GetMaxHealth();

        var padding = _mask.padding;
        padding.z = _maxRightMask * (1f - healthPercentage);
        _mask.padding = padding;
    }

    void Update()
    {
        if (_playerHealth != null)
        {
            SetValue(_playerHealth.GetCurrentHealth());
        }
    }
}
