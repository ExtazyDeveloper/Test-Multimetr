using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Обрабатывает взаимодействие с регулятором мультиметра: луч с камеры, подсветка при наведении (эмиссия),
/// прокрутка колесом для смены шага и обновление UI через <see cref="MultimeterDisplayView"/>.
/// </summary>
public sealed class MultimeterHoverEmissionController : MonoBehaviour
{
    private const float ReferenceLocalYForStep0 = 0f;

    [SerializeField] private Camera raycastCamera;
    [SerializeField] private Color hoverEmissionColor;
    [SerializeField] private float hoverEmissionIntensity;
    [SerializeField] private TMP_Text displayValueText;
    [SerializeField] private TMP_Text UIModeAndValueText;
    [SerializeField] private Renderer primaryDialRenderer;

    private MaterialPropertyBlock _block;
    private MultimeterDisplayView _view;

    private Renderer _hoverRenderer;
    private Color _baseEmission;

    private bool _dialRegistered;
    private int _dialStep;

    // Создаёт блок свойств материала и представление для текстов.
    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _view = new MultimeterDisplayView(displayValueText, UIModeAndValueText);
    }

    // При включении объекта в режиме игры синхронизирует диск и UI.
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        InitDialFromPrimary();
    }

    // На следующий кадр повторно инициализирует UI (после готовности TMP).
    private void Start()
    {
        StartCoroutine(RefreshUiNextFrame());
    }

    // Ждёт один кадр и вызывает полную инициализацию диска и показаний.
    private IEnumerator RefreshUiNextFrame()
    {
        yield return null;
        InitDialFromPrimary();
    }

    // Регистрирует текущее положение диска и обновляет отображаемые значения.
    private void InitDialFromPrimary()
    {
        RegisterDialIfNeeded(primaryDialRenderer);
        RefreshReadoutUi();
    }

    // Каждый кадр: луч по курсору, подсветка при наведении на диск, колесо для смены шага.
    private void Update()
    {
        if (Mouse.current == null || raycastCamera == null || primaryDialRenderer == null)
            return;

        var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!TryRaycastPrimaryDial(ray, out _))
        {
            ClearHoverHighlight();
            return;
        }

        if (primaryDialRenderer != _hoverRenderer)
        {
            ClearHoverHighlight();
            _hoverRenderer = primaryDialRenderer;
            CacheBaseEmission(primaryDialRenderer);
            RegisterDialIfNeeded(primaryDialRenderer);
            ApplyHoverHighlight(primaryDialRenderer);
            RefreshReadoutUi();
        }

        var scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY != 0f && _dialRegistered)
        {
            var dir = scrollY > 0f ? 1 : -1;
            var next = Mathf.Clamp(_dialStep + dir, 0, DialGeometry.MaxStepIndex(MultimeterReadoutCalculator.SlotCount));
            next = MultimeterReadoutCalculator.ClampStepIndex(next);
            if (next != _dialStep)
            {
                _dialStep = next;
                DialGeometry.ApplyLocalY(primaryDialRenderer.transform, ReferenceLocalYForStep0, _dialStep, MultimeterReadoutCalculator.SlotCount);
                RefreshReadoutUi();
            }
        }
    }

    // Запоминает исходный цвет эмиссии материала до подсветки.
    private void CacheBaseEmission(Renderer r)
    {
        _baseEmission = r.sharedMaterial.GetColor("_EmissionColor");
    }

    // Задаёт цвет эмиссии при наведении и включает ключевое слово EMISSION.
    private void ApplyHoverHighlight(Renderer r)
    {
        r.GetPropertyBlock(_block);
        var hoverEmission = hoverEmissionColor * hoverEmissionIntensity;
        _block.SetColor("_EmissionColor", hoverEmission);
        r.SetPropertyBlock(_block);
        r.sharedMaterial.EnableKeyword("_EMISSION");
    }

    // Находит ближайшее к камере попадание луча по коллайдеру, относящемуся к диску.
    private bool TryRaycastPrimaryDial(Ray ray, out RaycastHit hit)
    {
        hit = default;
        if (primaryDialRenderer == null) return false;

        var hits = Physics.RaycastAll(ray, Mathf.Infinity);
        var bestDist = float.MaxValue;
        var found = false;
        foreach (var h in hits)
        {
            if (!HitBelongsToDial(h, primaryDialRenderer)) continue;
            if (h.distance >= bestDist) continue;
            bestDist = h.distance;
            hit = h;
            found = true;
        }

        return found;
    }

    // Проверяет, что попадание относится к тому же объекту диска (сам или потомок/родитель).
    private static bool HitBelongsToDial(RaycastHit hit, Renderer dial)
    {
        var c = hit.collider.transform;
        var d = dial.transform;
        return c == d || c.IsChildOf(d) || d.IsChildOf(c);
    }

    // Один раз вычисляет шаг по углу диска, выравнивает поворот и помечает диск как зарегистрированный.
    private void RegisterDialIfNeeded(Renderer r)
    {
        if (r == null || r != primaryDialRenderer || _dialRegistered) return;

        _dialStep = DialGeometry.StepFromAngle(ReferenceLocalYForStep0, r.transform.localEulerAngles.y, MultimeterReadoutCalculator.SlotCount);
        _dialStep = MultimeterReadoutCalculator.ClampStepIndex(_dialStep);
        _dialRegistered = true;
        DialGeometry.ApplyLocalY(r.transform, ReferenceLocalYForStep0, _dialStep, MultimeterReadoutCalculator.SlotCount);
    }

    // Строит снимок показаний по текущему шагу и передаёт его во view или очищает тексты.
    private void RefreshReadoutUi()
    {
        if (!_view.HasAnyBinding) return;

        if (primaryDialRenderer == null || !_dialRegistered)
        {
            _view.Clear();
            return;
        }

        var snapshot = MultimeterReadoutCalculator.BuildSnapshot(_dialStep);
        _view.Render(snapshot);
    }

    // Восстанавливает исходную эмиссию и сбрасывает ссылку на подсвеченный рендерер.
    private void ClearHoverHighlight()
    {
        if (_hoverRenderer == null) return;

        _hoverRenderer.GetPropertyBlock(_block);
        _block.SetColor("_EmissionColor", _baseEmission);
        _hoverRenderer.SetPropertyBlock(_block);
        _hoverRenderer = null;
    }
}
