using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ForgettingBoxer.Knockout
{
    [DisallowMultipleComponent]
    public sealed class KnockoutSystem : MonoBehaviour
    {
        public static KnockoutSystem Instance { get; private set; }

        [Header("Rules")]
        [SerializeField, Min(1f)] private float countdownValue = 10f;
        [SerializeField, Range(0.01f, 0.999f)] private float timerDifficultyFactor = 0.9f;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 2f;
        [SerializeField] private int startingStars;

        [Header("Scene references")]
        [SerializeField] private GameObject player;
        [SerializeField] private MonoBehaviour[] disableWhileKnockedOut;
        [Tooltip("Invisible landing/launch point inside the counter.")]
        [SerializeField] private RectTransform starCounterPoint;
        [Tooltip("Point stars fly from when AddStars is called.")]
        [SerializeField] private RectTransform collectSpawnPoint;
        [SerializeField] private RectTransform flyingStarsRoot;
        [Tooltip("Inactive scene object used as the visual/clickable star template.")]
        [SerializeField] private Button starTemplate;
        [SerializeField] private CanvasGroup starCounterGroup;
        [SerializeField] private TMP_Text starCounterText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text statusText;

        [Header("Presentation")]
        [SerializeField, Min(0.05f)] private float collectFlightDuration = 0.5f;
        [SerializeField, Min(0.05f)] private float scatterDuration = 0.45f;
        [SerializeField] private Vector2 screenPadding = new(65f, 80f);

        [Header("Events")]
        public UnityEvent onKnockoutStarted;
        public UnityEvent onRecovered;
        public UnityEvent onGameOver;
        public UnityEvent onStarCountChanged;

        private readonly List<Button> activeStars = new();
        private readonly Dictionary<MonoBehaviour, bool> previousEnabledStates = new();
        private float previousTimeScale = 1f;
        private float invulnerableUntil;
        private int stars;
        private int knockoutCount;
        private bool isKnockedOut;
        private bool isGameOver;
        private bool worldPaused;
        private Coroutine knockoutRoutine;

        public int Stars => stars;
        public int KnockoutCount => knockoutCount;
        public bool IsKnockedOut => isKnockedOut;
        public bool IsGameOver => isGameOver;
        public bool IsInvulnerable => Time.unscaledTime < invulnerableUntil;
        public float TimerSpeed => 10f / (10f * Mathf.Pow(timerDifficultyFactor, knockoutCount));
        public float RecoveryTimeLimit => countdownValue / TimerSpeed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            stars = Mathf.Max(0, startingStars);
            if (!ValidateSceneReferences())
            {
                enabled = false;
                return;
            }
            starTemplate.gameObject.SetActive(false);
            RefreshHUD();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (worldPaused) RestoreWorld();
            Instance = null;
        }

        public void AddStar() => AddStars(1);

        public void AddStars(int amount)
        {
            if (amount <= 0 || isGameOver) return;
            stars += amount;
            RefreshHUD();
            onStarCountChanged?.Invoke();

            if (!isKnockedOut)
            {
                for (int i = 0; i < amount; i++)
                    StartCoroutine(AnimateCollectedStar(i * 0.06f));
            }
        }

        public void TakeDamage() => TakeDamage(1);

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || isKnockedOut || isGameOver || IsInvulnerable) return;
            // Damage grants a star without the collect animation: all accumulated
            // stars must immediately scatter from the counter instead.
            stars++;
            RefreshHUD();
            onStarCountChanged?.Invoke();
            BeginKnockout();
        }

        public void BeginKnockout()
        {
            if (isKnockedOut || isGameOver) return;
            if (stars <= 0) stars = 1;
            knockoutRoutine = StartCoroutine(KnockoutSequence());
        }

        public void ResetRun()
        {
            if (knockoutRoutine != null) StopCoroutine(knockoutRoutine);
            ClearActiveStars();
            RestoreWorld();
            stars = 0;
            knockoutCount = 0;
            isKnockedOut = false;
            isGameOver = false;
            invulnerableUntil = 0f;
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (statusText != null) statusText.text = string.Empty;
            RefreshHUD();
        }

        private IEnumerator KnockoutSequence()
        {
            isKnockedOut = true;
            SetPlayerKnockedOut(true);
            PauseWorld();
            onKnockoutStarted?.Invoke();

            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(Mathf.Max(0f, countdownValue)).ToString();
                timerText.gameObject.SetActive(true);
            }
            if (statusText != null) statusText.text = "KNOCKOUT! CLICK ALL STARS";
            yield return ScatterStars();

            float remaining = countdownValue;
            while (remaining > 0f && activeStars.Count > 0)
            {
                remaining -= Time.unscaledDeltaTime * TimerSpeed;
                if (timerText != null) timerText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
                yield return null;
            }

            if (activeStars.Count == 0)
                Recover();
            else
                GameOver();

            knockoutRoutine = null;
        }

        private IEnumerator ScatterStars()
        {
            ClearActiveStars();
            // Stars must be above every other UI graphic so no panel can steal clicks.
            flyingStarsRoot.SetAsLastSibling();
            int count = stars;
            Vector2 origin = LocalPointOf(starCounterPoint);
            for (int i = 0; i < count; i++)
            {
                Button star = CreateStarButton(origin, true);
                activeStars.Add(star);
                Vector2 target = RandomScreenPoint();
                StartCoroutine(MoveRect((RectTransform)star.transform, origin, target, scatterDuration));
            }
            yield return new WaitForSecondsRealtime(scatterDuration);
        }

        private Button CreateStarButton(Vector2 position, bool interactable)
        {
            Button button = Instantiate(starTemplate, flyingStarsRoot);
            button.name = interactable ? "Knockout Star" : "Collected Star";
            button.gameObject.SetActive(true);
            button.interactable = interactable;
            RectTransform rect = (RectTransform)button.transform;
            rect.anchoredPosition = position;
            rect.SetAsLastSibling();
            button.onClick.RemoveAllListeners();
            if (interactable) button.onClick.AddListener(() => RemoveKnockoutStar(button));
            return button;
        }

        private void RemoveKnockoutStar(Button star)
        {
            if (!isKnockedOut || !activeStars.Remove(star)) return;
            Destroy(star.gameObject);
        }

        private void Recover()
        {
            stars = 0;
            knockoutCount++;
            isKnockedOut = false;
            invulnerableUntil = Time.unscaledTime + invulnerabilityDuration;
            SetPlayerKnockedOut(false);
            RestoreWorld();
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (statusText != null) statusText.text = $"BACK UP!  INVULNERABLE {invulnerabilityDuration:0.#}s";
            RefreshHUD();
            onStarCountChanged?.Invoke();
            onRecovered?.Invoke();
        }

        private void GameOver()
        {
            isGameOver = true;
            isKnockedOut = false;
            ClearActiveStars();
            if (timerText != null) timerText.text = "0";
            if (statusText != null) statusText.text = "GAME OVER";
            onGameOver?.Invoke();
        }

        private void PauseWorld()
        {
            if (worldPaused) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            worldPaused = true;
        }

        private void RestoreWorld()
        {
            if (!worldPaused) return;
            Time.timeScale = previousTimeScale;
            worldPaused = false;
        }

        private void SetPlayerKnockedOut(bool knockedOut)
        {
            if (player != null)
            {
                Animator animator = player.GetComponentInChildren<Animator>();
                if (animator != null) animator.SetBool("KnockedOut", knockedOut);
            }

            if (knockedOut)
            {
                previousEnabledStates.Clear();
                foreach (MonoBehaviour behaviour in disableWhileKnockedOut)
                {
                    if (behaviour == null) continue;
                    previousEnabledStates[behaviour] = behaviour.enabled;
                    behaviour.enabled = false;
                }
            }
            else
            {
                foreach (var pair in previousEnabledStates)
                    if (pair.Key != null) pair.Key.enabled = pair.Value;
                previousEnabledStates.Clear();
            }
        }

        private IEnumerator AnimateCollectedStar(float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            Button visual = CreateStarButton(LocalPointOf(collectSpawnPoint), false);
            yield return MoveRect((RectTransform)visual.transform, LocalPointOf(collectSpawnPoint), LocalPointOf(starCounterPoint), collectFlightDuration);
            if (visual != null) Destroy(visual.gameObject);
        }

        private Vector2 LocalPointOf(RectTransform point)
        {
            return flyingStarsRoot.InverseTransformPoint(point.position);
        }

        private IEnumerator MoveRect(RectTransform rect, Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            while (rect != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }
            if (rect != null) rect.anchoredPosition = to;
        }

        private Vector2 RandomScreenPoint()
        {
            Rect rect = flyingStarsRoot.rect;
            Vector2 actualStarSize = ((RectTransform)starTemplate.transform).rect.size;
            float halfW = actualStarSize.x * 0.5f;
            float halfH = actualStarSize.y * 0.5f;
            float minX = rect.xMin + screenPadding.x + halfW;
            float maxX = rect.xMax - screenPadding.x - halfW;
            float minY = rect.yMin + screenPadding.y + halfH;
            float maxY = rect.yMax - screenPadding.y - halfH;
            return new Vector2(
                minX < maxX ? UnityEngine.Random.Range(minX, maxX) : 0f,
                minY < maxY ? UnityEngine.Random.Range(minY, maxY) : 0f);
        }

        private void RefreshHUD()
        {
            if (starCounterText != null) starCounterText.text = $"★  {stars}";
            if (starCounterGroup != null)
            {
                bool visible = stars > 0;
                starCounterGroup.alpha = visible ? 1f : 0f;
                starCounterGroup.interactable = false;
                starCounterGroup.blocksRaycasts = false;
            }
        }

        private void ClearActiveStars()
        {
            foreach (Button star in activeStars)
                if (star != null) Destroy(star.gameObject);
            activeStars.Clear();
        }

        private bool ValidateSceneReferences()
        {
            bool valid = starCounterPoint != null && collectSpawnPoint != null && flyingStarsRoot != null &&
                         starTemplate != null && starCounterGroup != null && starCounterText != null &&
                         timerText != null && statusText != null;
            if (!valid)
                Debug.LogError("KnockoutSystem: assign all Scene references. Use Tools/Knockout/Create or Rebuild Test Scene as an example.", this);
            return valid;
        }
    }
}
