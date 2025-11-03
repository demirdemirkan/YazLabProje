using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Health playerHealth; // Player üzerindeki Health
    public Slider slider;

    void Awake()
    {
        if (playerHealth)
            playerHealth.OnHealthChanged += OnHealthChanged;
    }

    void OnDestroy()
    {
        if (playerHealth)
            playerHealth.OnHealthChanged -= OnHealthChanged;
    }

    void Start()
    {
        if (playerHealth && slider)
        {
            slider.maxValue = playerHealth.Max;
            slider.value = playerHealth.Current;
        }
    }

    void OnHealthChanged(float current, float max)
    {
        if (slider)
        {
            slider.maxValue = max;
            slider.value = current;
        }
    }
}
