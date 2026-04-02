using UnityEngine;

/// <summary>
/// Вспомогательные расчёты дискретного поворота регулятора по дуге 180°: шаг в градусах, индекс по углу, применение угла к объекту.
/// </summary>
public static class DialGeometry
{
    // Возвращает максимальный индекс шага (0-based) для заданного числа слотов.
    public static int MaxStepIndex(int totalSlots)
    {
        return Mathf.Max(2, totalSlots) - 1;
    }

    // Угол между соседними положениями при равномерном распределении по 180°.
    public static float StepDegrees(int totalSlots)
    {
        return 180f / (Mathf.Max(2, totalSlots) - 1);
    }

    // Вычисляет индекс шага по разнице между текущим и опорным углом поворота по Y.
    public static int StepFromAngle(float referenceY, float currentY, int totalSlots)
    {
        var maxIndex = MaxStepIndex(totalSlots);
        var stepDeg = StepDegrees(totalSlots);
        var delta = Mathf.DeltaAngle(referenceY, currentY);
        var step = Mathf.RoundToInt(delta / stepDeg);
        return Mathf.Clamp(step, 0, maxIndex);
    }

    // Устанавливает локальный угол Y трансформа в соответствии с номером шага.
    public static void ApplyLocalY(Transform target, float referenceY, int step, int totalSlots)
    {
        var yAngle = referenceY + step * StepDegrees(totalSlots);
        var currentEuler = target.localEulerAngles;
        target.localEulerAngles = new Vector3(currentEuler.x, yAngle, currentEuler.z);
    }
}
