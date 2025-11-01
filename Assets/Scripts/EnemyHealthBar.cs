using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public Health health;            // düþmanýn Health'i
    public Slider slider;            // world-space slider
    public Transform target;         // kafanýn üstü
    public Vector3 offset = new Vector3(0f, 2.0f, 0f);
    public Camera cam;

    void Awake()
    {
        if (!health) health = GetComponentInParent<Health>();
        if (!cam) cam = Camera.main;
        if (health)
            health.OnHealthChanged += OnHealthChanged;
    }

    void OnDestroy()
    {
        if (health)
            health.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(float current, float max)
    {
        if (slider)
        {
            slider.maxValue = max;
            slider.value = current;
        }
    }

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (target)
            transform.position = target.position + offset;

        // Kameraya dön
        if (cam)
            transform.forward = cam.transform.forward;
    }
}
