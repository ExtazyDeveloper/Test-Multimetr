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
	private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private Camera raycastCamera;
	[ColorUsage(false, true)]
    [SerializeField] private Color hoverEmissionColor;
    [SerializeField] private float hoverEmissionIntensity;
    [SerializeField] private TMP_Text displayValueText;
    [SerializeField] private TMP_Text uiModeAndValueText;
    [SerializeField] private Renderer primaryDialRenderer;
	[SerializeField] private float raycastMaxDistance = 100f;

    private MaterialPropertyBlock _block;
    private MultimeterDisplayView _view;

    private Color _baseEmission;
	private Color[] _baseEmissionPerMat;
	private bool _prevEmissionKeywordEnabled;
	private bool[] _prevEmissionKeywordEnabledPerMat;

	private bool _isHovered;
    private int _dialStep;
	private int _dialLayerMask;

    // Создаёт блок свойств материала и представление для текстов.
    private void Awake()
    {
		if (raycastCamera == null || primaryDialRenderer == null ||
			displayValueText == null || uiModeAndValueText == null)
		{
			enabled = false;
			return;
		}

		_block = new MaterialPropertyBlock();
		_view = new MultimeterDisplayView(displayValueText, uiModeAndValueText);
    }

    // При включении объекта в режиме игры синхронизирует диск и UI.
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
		InitDialFromPrimary();
		// Обычно достаточно, чтобы UI был готов в тот же кадр
		displayValueText.ForceMeshUpdate();
		uiModeAndValueText.ForceMeshUpdate();
		RefreshReadoutUi();
    }

    // Регистрирует текущее положение диска и обновляет отображаемые значения.
    private void InitDialFromPrimary()
    {
		_dialStep = MultimeterReadoutCalculator.ClampStepIndex(
			DialGeometry.StepFromAngle(ReferenceLocalYForStep0, primaryDialRenderer.transform.localEulerAngles.y, MultimeterReadoutCalculator.SlotCount));
		DialGeometry.ApplyLocalY(primaryDialRenderer.transform, ReferenceLocalYForStep0, _dialStep, MultimeterReadoutCalculator.SlotCount);

		// Кэш базовой эмиссии с инстанса материала
		var mats = primaryDialRenderer.materials;
		var count = mats.Length;
		if (count == 0)
		{
			var mat = primaryDialRenderer.material;
			_baseEmission = mat.GetColor(EmissionColorId);
			_prevEmissionKeywordEnabled = mat.IsKeywordEnabled("_EMISSION");
			_baseEmissionPerMat = null;
			_prevEmissionKeywordEnabledPerMat = null;
		}
		else
		{
			_baseEmissionPerMat = new Color[count];
			_prevEmissionKeywordEnabledPerMat = new bool[count];
			for (int i = 0; i < count; i++)
			{
				_baseEmissionPerMat[i] = mats[i].GetColor(EmissionColorId);
				_prevEmissionKeywordEnabledPerMat[i] = mats[i].IsKeywordEnabled("_EMISSION");
			}
			// На случай использования SetPropertyBlock без индекса как фоллбэка
			_baseEmission = _baseEmissionPerMat[0];
			_prevEmissionKeywordEnabled = _prevEmissionKeywordEnabledPerMat[0];
		}

		// Формируем маску слоёв из всех коллайдеров диска (и детей)
		_dialLayerMask = 0;
		var colliders = primaryDialRenderer.GetComponentsInChildren<Collider>(true);
		for (int i = 0; i < colliders.Length; i++)
		{
			_dialLayerMask |= 1 << colliders[i].gameObject.layer;
		}
		// Фоллбэк: если коллайдеров нет, используем слой объекта рендера
		if (_dialLayerMask == 0)
		{
			_dialLayerMask = 1 << primaryDialRenderer.gameObject.layer;
		}
    }

    // Каждый кадр: луч по курсору, подсветка при наведении на диск, колесо для смены шага.
    private void Update()
    {
		if (Mouse.current == null || raycastCamera == null || primaryDialRenderer == null)
            return;

        var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

		var isHit = Physics.Raycast(ray, out _, raycastMaxDistance, _dialLayerMask, QueryTriggerInteraction.Collide);
		if (isHit != _isHovered) SetHover(isHit);

        var scrollY = Mouse.current.scroll.ReadValue().y;
		if (scrollY != 0f)
        {
            var dir = scrollY > 0f ? 1 : -1;
			var next = MultimeterReadoutCalculator.ClampStepIndex(_dialStep + dir);
            if (next != _dialStep)
            {
                _dialStep = next;
                DialGeometry.ApplyLocalY(primaryDialRenderer.transform, ReferenceLocalYForStep0, _dialStep, MultimeterReadoutCalculator.SlotCount);
                RefreshReadoutUi();
            }
        }
    }

	private void SetHover(bool hovered)
    {
		_isHovered = hovered;

		// Обновляем цвет через MaterialPropertyBlock для всех материалов рендера
		var mats = primaryDialRenderer.materials;
		var count = mats.Length;
		if (count <= 1)
		{
			primaryDialRenderer.GetPropertyBlock(_block);
			var color = hovered ? hoverEmissionColor * hoverEmissionIntensity : _baseEmission;
			_block.SetColor(EmissionColorId, color);
			primaryDialRenderer.SetPropertyBlock(_block);

			// Управляем ключевым словом _EMISSION у инстанса материала
			var mat = primaryDialRenderer.material;
			var isEnabled = mat.IsKeywordEnabled("_EMISSION");
			if (hovered)
			{
				if (!isEnabled) mat.EnableKeyword("_EMISSION");
			}
			else
			{
				if (isEnabled && !_prevEmissionKeywordEnabled)
					mat.DisableKeyword("_EMISSION");
			}
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				primaryDialRenderer.GetPropertyBlock(_block, i);
				var baseCol = (_baseEmissionPerMat != null && i < _baseEmissionPerMat.Length) ? _baseEmissionPerMat[i] : _baseEmission;
				var color = hovered ? hoverEmissionColor * hoverEmissionIntensity : baseCol;
				_block.SetColor(EmissionColorId, color);
				primaryDialRenderer.SetPropertyBlock(_block, i);

				var mat = mats[i];
				var isEnabled = mat.IsKeywordEnabled("_EMISSION");
				if (hovered)
				{
					if (!isEnabled) mat.EnableKeyword("_EMISSION");
				}
				else
				{
					var wasEnabled = (_prevEmissionKeywordEnabledPerMat != null && i < _prevEmissionKeywordEnabledPerMat.Length)
						? _prevEmissionKeywordEnabledPerMat[i]
						: _prevEmissionKeywordEnabled;
					if (isEnabled && !wasEnabled) mat.DisableKeyword("_EMISSION");
				}
			}
		}
    }

    // Строит снимок показаний по текущему шагу и передаёт его во view или очищает тексты.
    private void RefreshReadoutUi()
    {
        if (!_view.HasAnyBinding) return;

        var snapshot = MultimeterReadoutCalculator.BuildSnapshot(_dialStep);
        _view.Render(snapshot);
    }
}
