using System.Globalization;
using UnityEngine;

public static class MultimeterReadoutCalculator
{
    private const float KnownResistanceOhm = 1000f;
    private const float KnownPowerWatt = 400f;

    private static readonly string[] SlotLabels =
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

    public static int SlotCount => SlotLabels.Length;

    public static int ClampStepIndex(int step)
    {
        return Mathf.Clamp(step, 0, SlotCount - 1);
    }

    public static MultimeterReadoutSnapshot BuildSnapshot(int step)
    {
        step = ClampStepIndex(step);
        return new MultimeterReadoutSnapshot(SlotLabels[step], FormatF2(ReadingsByStep[step]));
    }

    private static float[] CreateReadingsByStep()
    {
        return
        [
            0f,
            Mathf.Sqrt(KnownPowerWatt * KnownResistanceOhm),
            0.01f,
            KnownResistanceOhm,
            0f,
            Mathf.Sqrt(KnownPowerWatt / KnownResistanceOhm),
            0f
        ];
    }

    private static string FormatF2(float value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}

public readonly struct MultimeterReadoutSnapshot
{
    public MultimeterReadoutSnapshot(string mode, string reading)
    {
        Mode = mode;
        Reading = reading;
    }

    public string Mode { get; }
    public string Reading { get; }
}
