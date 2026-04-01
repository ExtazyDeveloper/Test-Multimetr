using System.Globalization;
using UnityEngine;

/// <summary>
/// Статическая модель показаний мультиметра: подписи режимов, числовые значения по шагу и сборка данных для UI.
/// Число режимов задаётся константой <see cref="SlotCount"/>.
/// </summary>
public static class MultimeterReadoutCalculator
{
    // Фиксированное число положений переключателя (согласовано с геометрией диска).
    public const int SlotCount = 7;

    private const float KnownResistanceOhm = 1000f;
    private const float KnownPowerWatt = 400f;

    private static readonly string[] SlotLabels = new string[SlotCount]
    {
        "OFF",
        "V-",
        "V~",
        "Ω",
        "Hz",
        "A~",
        "NCV"
    };

    private static readonly float[] ReadingsByStep = CreateReadingsByStep();

    // Сжимает индекс шага в допустимый диапазон [0, SlotCount - 1].
    public static int ClampStepIndex(int step)
    {
        return Mathf.Clamp(step, 0, SlotCount - 1);
    }

    // Формирует снимок для UI: режим, форматированное показание и объединённая строка.
    public static MultimeterReadoutSnapshot BuildSnapshot(int step)
    {
        step = ClampStepIndex(step);
        var mode = SlotLabels[step];
        var reading = FormatReadingForStep(step);
        return new MultimeterReadoutSnapshot(mode, reading, BuildCombinedLine(mode, reading));
    }

    // Создаёт таблицу демонстрационных показаний по индексу шага.
    private static float[] CreateReadingsByStep()
    {
        return new float[SlotCount]
        {
            0f,
            Mathf.Sqrt(KnownPowerWatt * KnownResistanceOhm),
            0.01f,
            KnownResistanceOhm,
            0f,
            Mathf.Sqrt(KnownPowerWatt / KnownResistanceOhm),
            0f
        };
    }

    // Возвращает строку показания для шага после клампа (индекс валиден).
    private static string FormatReadingForStep(int step)
    {
        return FormatF2(ReadingsByStep[step]);
    }

    // Собирает двухстрочный текст режима и показания.
    private static string BuildCombinedLine(string mode, string reading)
    {
        return "Режим: " + mode + "\nПоказание: " + reading;
    }

    // Форматирует число с двумя знаками после запятой (инвариантная культура).
    private static string FormatF2(float value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Неизменяемый набор строк для отображения: режим, показание и готовая комбинированная подпись.
/// </summary>
public readonly struct MultimeterReadoutSnapshot
{
    // Создаёт снимок из трёх строк для view.
    public MultimeterReadoutSnapshot(string mode, string reading, string combinedLine)
    {
        Mode = mode;
        Reading = reading;
        CombinedLine = combinedLine;
    }

    // Текущий режим (подпись слота).
    public string Mode { get; }

    // Строка числового показания.
    public string Reading { get; }

    // Две строки: режим и показание для комбинированного поля.
    public string CombinedLine { get; }
}
