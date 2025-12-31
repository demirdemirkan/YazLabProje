using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public Health health;     // düþmanýn Health'i
    public Slider slider;     // world-space slider
    public Transform target;  // kafa boþ objesi
    public Vector3 offset = new Vector3(0f, 2.0f, 0f);
    public Camera cam;

    void Awake()
    {
        if (!health) health = GetComponentInParent<Health>();
        if (!cam) cam = Camera.main;
        if (health) health.OnHealthChanged += OnHealthChanged;
    }

    void OnEnable()
    {
        // pooling durumunda tekrar etkinleþince de güncelle
        ForceRefresh();
    }

    void Start()
    {
        // sahne ilk açýldýðýnda hemen doðru deðeri göster
        ForceRefresh();
    }

    void OnDestroy()
    {
        if (health) health.OnHealthChanged -= OnHealthChanged;
    }

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (target) transform.position = target.position + offset;
        if (cam) transform.forward = cam.transform.forward; // her zaman kameraya dön
    }

    void OnHealthChanged(float current, float max)
    {
        if (!slider) return;
        slider.maxValue = max;
        slider.value = current;
    }

    void ForceRefresh()
    {
        if (!health || !slider) return;
        slider.maxValue = health.Max;
        slider.value = health.Current;
    }
}
