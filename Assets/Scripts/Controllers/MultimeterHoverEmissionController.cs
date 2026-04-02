using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MultimeterHoverEmissionController : MonoBehaviour
{
    private const float ReferenceDialAngleYForStep0 = 0f;

    [SerializeField] private Camera raycastCamera;
    [ColorUsage(false, true)]
    [SerializeField] private Color hoverEmissionColor;
    [SerializeField] private float hoverEmissionIntensity;
    [SerializeField] private TMP_Text displayValueText;
    [SerializeField] private TMP_Text uiModeAndValueText;
    [SerializeField] private Renderer primaryDialRenderer;
    [SerializeField] private float raycastMaxDistance = 100f;

    private MultimeterDisplayView _view;
    private DialHoverHighlighter _hoverHighlighter;
    private int _dialStep;
    private int _dialColliderLayerMask;

    private void Awake()
    {
        if (raycastCamera == null || primaryDialRenderer == null ||
            displayValueText == null || uiModeAndValueText == null)
        {
            enabled = false;
            return;
        }

        _view = new MultimeterDisplayView(displayValueText, uiModeAndValueText);
        _hoverHighlighter = new DialHoverHighlighter(primaryDialRenderer, hoverEmissionColor, hoverEmissionIntensity);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        InitializeDialState();
        displayValueText.ForceMeshUpdate();
        uiModeAndValueText.ForceMeshUpdate();
        RenderReadout();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        _hoverHighlighter?.SetHover(false);
    }

    private void InitializeDialState()
    {
        _dialStep = MultimeterReadoutCalculator.ClampStepIndex(
            DialGeometry.StepFromAngle(ReferenceDialAngleYForStep0, primaryDialRenderer.transform.localEulerAngles.y, MultimeterReadoutCalculator.SlotCount));
        DialGeometry.ApplyLocalY(primaryDialRenderer.transform, ReferenceDialAngleYForStep0, _dialStep, MultimeterReadoutCalculator.SlotCount);
        _hoverHighlighter.InitializeBaseEmission();

        _dialColliderLayerMask = 0;
        var colliders = primaryDialRenderer.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            _dialColliderLayerMask |= 1 << colliders[i].gameObject.layer;

        if (_dialColliderLayerMask == 0)
            _dialColliderLayerMask = 1 << primaryDialRenderer.gameObject.layer;
    }

    private void Update()
    {
        if (Mouse.current == null || raycastCamera == null || primaryDialRenderer == null)
            return;

        var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        var isHovered = Physics.Raycast(ray, out _, raycastMaxDistance, _dialColliderLayerMask, QueryTriggerInteraction.Collide);
        _hoverHighlighter.SetHover(isHovered);

        var scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY != 0f)
        {
            var stepDelta = scrollY > 0f ? 1 : -1;
            var nextDialStep = MultimeterReadoutCalculator.ClampStepIndex(_dialStep + stepDelta);
            if (nextDialStep != _dialStep)
            {
                _dialStep = nextDialStep;
                DialGeometry.ApplyLocalY(primaryDialRenderer.transform, ReferenceDialAngleYForStep0, _dialStep, MultimeterReadoutCalculator.SlotCount);
                RenderReadout();
            }
        }
    }

    private void RenderReadout()
    {
        if (!_view.HasAnyBinding) return;
        _view.Render(MultimeterReadoutCalculator.BuildSnapshot(_dialStep));
    }
}
