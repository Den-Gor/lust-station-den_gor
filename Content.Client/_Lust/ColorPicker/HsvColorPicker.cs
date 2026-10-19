using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Lust.ColorPicker;

/// <summary>
/// Готовый пикер HSV
/// </summary>
public sealed class HsvColorPicker : BoxContainer
{
    /// <summary>
    /// Квадрат S/V
    /// </summary>
    public ColorSvSquare Square => _square;

    /// <summary>
    /// Полоска Hue
    /// </summary>
    public ColorHueStrip Strip => _strip;

    private readonly ColorSvSquare _square;
    private readonly ColorHueStrip _strip;

    /// <summary>
    /// Текущий HSV
    /// </summary>
    public Vector4 Hsv => new(_square.H, _square.S, _square.V, 1f);

    /// <summary>
    /// Текущий цвет
    /// </summary>
    public Color CurrentColor => Color.FromHsv(Hsv);

    /// <summary>
    /// Ивент вызываемый при изменине hue или S/V квадрата возвращает hsv
    /// </summary>
    public event Action<float, float, float>? HsvChanged;

    public HsvColorPicker()
    {
        Orientation = LayoutOrientation.Vertical;

        AddChild(_square = new ColorSvSquare
        {
            MinSize = new Vector2(200, 200),
            HorizontalExpand = true,
            VerticalExpand = true,
        });
        AddChild(_strip = new ColorHueStrip
        {
            MinSize = new Vector2(200, 24),
            HorizontalExpand = true,
        });

        _square.HsvChanged += (h, s, v) => HsvChanged?.Invoke(h, s, v);
        _strip.HueChanged += OnHueChanged;
    }

    /// <summary>
    /// Установить цвет снаруже. Событие <see cref="HsvChanged"/> не вызывается
    /// </summary>
    public void SetHsv(float h, float s, float v)
    {
        _square.SetHsv(h, s, v);
        _strip.SetHue(h);
    }

    /// <summary>
    /// При изменение отенка перестраиваем куб
    /// </summary>
    private void OnHueChanged(float h)
    {
        _square.SetHue(h);
        HsvChanged?.Invoke(h, _square.S, _square.V);
    }
}
