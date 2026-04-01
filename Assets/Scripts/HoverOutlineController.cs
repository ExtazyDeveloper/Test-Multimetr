using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HoverOutlineController : MonoBehaviour
{
    // Камера, из которой формируется луч в позицию курсора
    [SerializeField] private Camera raycastCamera;
    // Маска слоёв: наведение обрабатываем только по объектам регулятора
    [SerializeField] private LayerMask regulatorMask;
    // Цвет эмиссии при наведении (множитель к существующей карте эмиссии)
    [SerializeField] private Color hoverEmissionColor;
    // Интенсивность эмиссии при наведении (в линейном пространстве; конвертируем в gamma)
    [SerializeField] private float hoverEmissionIntensity;
    [SerializeField] private float rotationSpeedY = 120f;

    // Текущий подсвеченный рендерер
    private Renderer currentRenderer;
    // Общий PropertyBlock, чтобы не создавать новые объекты каждый кадр
    private MaterialPropertyBlock block;
    // Запоминаем исходный эмиссивный цвет, чтобы корректно восстановить
    private Color baseEmission;

    private void Awake()
    {
        // Инициализируем один общий блок свойств для переиспользования
        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        // Получаем позицию курсора из Input System и строим луч из камеры
        var mousePosition = Mouse.current.position.ReadValue();
        var ray = raycastCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, regulatorMask))
        {
            // Берём ближайший релевантный Renderer (коллайдер может висеть не на самом рендерере)
            var targetRenderer = hit.collider.GetComponent<Renderer>();
            if (targetRenderer != currentRenderer)
            {
                ClearCurrent();
                currentRenderer = targetRenderer;
                // Сохраняем базовый эмиссивный цвет материала (текстура эмиссии остаётся нетронутой)
                baseEmission = currentRenderer.sharedMaterial.GetColor("_EmissionColor");

                // Усиливаем эмиссию через MaterialPropertyBlock (не создаём копий материалов)
                currentRenderer.GetPropertyBlock(block);
                var hoverEmission = hoverEmissionColor * Mathf.LinearToGammaSpace(hoverEmissionIntensity);
                block.SetColor("_EmissionColor", hoverEmission);
                currentRenderer.SetPropertyBlock(block);
                // На всякий случай включаем ключевое слово эмиссии у шейдера
                currentRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            }

            var scrollY = Mouse.current.scroll.ReadValue().y;
            if (scrollY != 0f)
            {
                targetRenderer.transform.Rotate(0f, scrollY * rotationSpeedY * Time.deltaTime, 0f, Space.Self);
            }
            return;
        }

        ClearCurrent();
    }

    private void ClearCurrent()
    {
        if (currentRenderer == null) return;

        // Возвращаем исходное эмиссивное значение и сбрасываем текущую ссылку
        currentRenderer.GetPropertyBlock(block);
        block.SetColor("_EmissionColor", baseEmission);
        currentRenderer.SetPropertyBlock(block);
        currentRenderer = null;
    }
}

