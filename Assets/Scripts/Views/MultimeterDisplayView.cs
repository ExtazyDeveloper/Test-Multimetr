using TMPro;

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
            _combined.text = BuildCombinedLine(snapshot.Mode, snapshot.Reading);
    }

    // Очищает оба привязанных текста.
    public void Clear()
    {
        if (_reading != null)
            _reading.text = string.Empty;
        if (_combined != null)
            _combined.text = string.Empty;
    }

    private static string BuildCombinedLine(string mode, string reading)
    {
        return "Режим: " + mode + "\nПоказание: " + reading;
    }
}
