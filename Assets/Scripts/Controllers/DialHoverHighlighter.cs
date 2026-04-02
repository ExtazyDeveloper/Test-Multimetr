using UnityEngine;

public sealed class DialHoverHighlighter
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly Renderer _renderer;
    private readonly Color _hoverEmissionColor;
    private readonly float _hoverEmissionIntensity;
    private readonly MaterialPropertyBlock _block = new();

    private Color _baseEmission;
    private Color[] _baseEmissionPerMat;
    private bool _prevEmissionKeywordEnabled;
    private bool[] _prevEmissionKeywordEnabledPerMat;
    private bool _isHovered;

    public DialHoverHighlighter(Renderer renderer, Color hoverEmissionColor, float hoverEmissionIntensity)
    {
        _renderer = renderer;
        _hoverEmissionColor = hoverEmissionColor;
        _hoverEmissionIntensity = hoverEmissionIntensity;
    }

    public void InitializeBaseEmission()
    {
        var materials = _renderer.materials;
        var materialCount = materials.Length;

        if (materialCount == 0)
        {
            var mat = _renderer.material;
            _baseEmission = mat.GetColor(EmissionColorId);
            _prevEmissionKeywordEnabled = mat.IsKeywordEnabled("_EMISSION");
            _baseEmissionPerMat = null;
            _prevEmissionKeywordEnabledPerMat = null;
            return;
        }

        _baseEmissionPerMat = new Color[materialCount];
        _prevEmissionKeywordEnabledPerMat = new bool[materialCount];
        for (int i = 0; i < materialCount; i++)
        {
            _baseEmissionPerMat[i] = materials[i].GetColor(EmissionColorId);
            _prevEmissionKeywordEnabledPerMat[i] = materials[i].IsKeywordEnabled("_EMISSION");
        }

        _baseEmission = _baseEmissionPerMat[0];
        _prevEmissionKeywordEnabled = _prevEmissionKeywordEnabledPerMat[0];
    }

    public void SetHover(bool hovered)
    {
        if (_isHovered == hovered) return;
        _isHovered = hovered;

        var materials = _renderer.materials;
        var materialCount = materials.Length;
        if (materialCount <= 1)
        {
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, hovered ? _hoverEmissionColor * _hoverEmissionIntensity : _baseEmission);
            _renderer.SetPropertyBlock(_block);

            var mat = _renderer.material;
            if (hovered)
            {
                if (!mat.IsKeywordEnabled("_EMISSION")) mat.EnableKeyword("_EMISSION");
            }
            else if (mat.IsKeywordEnabled("_EMISSION") && !_prevEmissionKeywordEnabled)
            {
                mat.DisableKeyword("_EMISSION");
            }
            return;
        }

        for (int i = 0; i < materialCount; i++)
        {
            _renderer.GetPropertyBlock(_block, i);
            var baseColor = (_baseEmissionPerMat != null && i < _baseEmissionPerMat.Length) ? _baseEmissionPerMat[i] : _baseEmission;
            _block.SetColor(EmissionColorId, hovered ? _hoverEmissionColor * _hoverEmissionIntensity : baseColor);
            _renderer.SetPropertyBlock(_block, i);

            var mat = materials[i];
            if (hovered)
            {
                if (!mat.IsKeywordEnabled("_EMISSION")) mat.EnableKeyword("_EMISSION");
            }
            else
            {
                var wasEnabled = (_prevEmissionKeywordEnabledPerMat != null && i < _prevEmissionKeywordEnabledPerMat.Length)
                    ? _prevEmissionKeywordEnabledPerMat[i]
                    : _prevEmissionKeywordEnabled;
                if (mat.IsKeywordEnabled("_EMISSION") && !wasEnabled) mat.DisableKeyword("_EMISSION");
            }
        }
    }
}
