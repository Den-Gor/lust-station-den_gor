using System.Numerics;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.ColorPicker;

/// <summary>
/// Запомненный цвет вместе с тем положением маркера, которое его выбрало.
/// </summary>
/// <remarks>
/// Внимание востановление цвета hsv по Color невозможно, так как конвертация не точная, поэтому храню ещё данные о Hsv
/// </remarks>
public sealed class ColorSlot
{
    /// <summary>
    /// Цвет
    /// </summary>
    public Color Color { get; private set; }

    /// <summary>
    /// HSV цвета
    /// </summary>
    public Vector4 Hsv { get; private set; }

    public ColorSlot(Color color, float hue = 0f)
    {
        Color = color;
        Hsv = BuildHsv(color, hue);
    }

    /// <summary>
    /// Установка цвета снаружи
    /// </summary>
    public void SetExternal(Color color)
    {
        Color = color;
        Hsv = BuildHsv(color, Hsv.X);
    }

    /// <summary>
    /// Установка цвета по Hsv
    /// </summary>
    public void SetHsv(float h, float s, float v)
    {
        Hsv = new Vector4(h, s, v, 1f);
        Color = Color.FromHsv(Hsv);
    }

    /// <summary>
    /// Восстановить HSV из цвета.
    /// </summary>
    private static Vector4 BuildHsv(Color color, float keepHue)
    {
        var hsv = Color.ToHsv(color);
        var achromatic = hsv.Y <= 0.02f || hsv.Z <= 0.02f;
        return new Vector4(achromatic ? keepHue : hsv.X, hsv.Y, hsv.Z, 1f);
    }
}
