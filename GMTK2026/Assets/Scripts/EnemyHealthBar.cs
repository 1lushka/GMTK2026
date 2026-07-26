using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private float barWidth = 1f;
    [SerializeField] private float barHeight = 0.15f;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private bool lookAtCamera = true;

    private HealthComponent health;
    private Canvas canvas;
    private Slider slider;
    private Image fillImage;
    private bool isDead;

    private void Start()
    {
        health = GetComponent<HealthComponent>();
        if (health == null)
        {
            Debug.LogWarning("EnemyHealthBar: No HealthComponent found on " + gameObject.name);
            enabled = false;
            return;
        }

        CreateHealthBar();

        health.onDamaged.AddListener(OnHealthChanged);
        health.onDeath.AddListener(OnDeath);
        OnHealthChanged(0);
    }

    private void CreateHealthBar()
    {
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        canvasObj.transform.localRotation = Quaternion.identity;

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth, barHeight);
        canvasRect.localScale = Vector3.one;

        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = health.MaxHealth;
        slider.value = health.CurrentHealth;
        slider.interactable = false;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.gray;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(sliderObj.transform, false);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fullColor;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.handleRect = null;
    }

    private void OnHealthChanged(int damage = 0)
    {
        if (slider != null && !isDead)
        {
            slider.value = health.CurrentHealth;
            if (fillImage != null)
                fillImage.color = Color.Lerp(emptyColor, fullColor, health.CurrentHealth / (float)health.MaxHealth);
        }
    }

    private void OnDeath()
    {
        isDead = true;
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (lookAtCamera && canvas != null && !isDead)
        {
            canvas.transform.LookAt(Camera.main.transform);
            canvas.transform.Rotate(0, 180, 0);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDamaged.RemoveListener(OnHealthChanged);
            health.onDeath.RemoveListener(OnDeath);
        }
    }
}