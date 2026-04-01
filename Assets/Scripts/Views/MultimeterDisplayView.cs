using TMPro;

/// <summary>
/// Отображает числовое показание и двухстрочный блок «режим + показание» в назначенных TMP_Text без логики расчёта.
/// </summary>
public sealed class MultimeterDisplayView
{
    private readonly TMP_Text _reading;
    private readonly TMP_Text _combined;

    // Привязывает поля к текстовым компонентам (допускается null для необязательных).
    public MultimeterDisplayView(TMP_Text reading, TMP_Text combined)
    {
        _reading = reading;
        _combined = combined;
    }

    // True, если задан хотя бы один из текстов для вывода.
    public bool HasAnyBinding => _reading != null || _combined != null;

    // Записывает в UI строки из снимка показаний.
    public void Render(in MultimeterReadoutSnapshot snapshot)
    {
        if (_reading != null)
            _reading.text = snapshot.Reading;
        if (_combined != null)
            _combined.text = snapshot.CombinedLine;
    }

    // Очищает оба привязанных текста.
    public void Clear()
    {
        if (_reading != null)
            _reading.text = string.Empty;
        if (_combined != null)
            _combined.text = string.Empty;
    }
}
