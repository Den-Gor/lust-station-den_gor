using System.Numerics;

namespace Content.Client._Lust.Canvas;

public enum CanvasTool : byte { Pen, Brush, Fill, Eraser, Eyedropper }

public sealed class CanvasToolState
{
    public CanvasTool Tool { get; set; } = CanvasTool.Pen;
    public Color Color { get; set; } = Color.Black;
    public int Size { get; set; } = 2;
    public float Opacity { get; set; } = 1f;

    // Помним H, S, V отдельно. Где маркер — такие и цифры.
    // У белого и чёрного оттенка нет, поэтому Hue просто не трогаем, он старый.
    public float Hue { get; set; }
    public float Saturation { get; set; }
    public float Brightness { get; set; }

    public const int SlotCount = 12;
    public readonly List<Color> SavedColors = new();
    public int SelectedSlot { get; set; }

    public event Action? Changed;
    public void Touch() => Changed?.Invoke();

    public CanvasToolState()
    {
        SavedColors.Add(Color.Black);
        for (var i = 1; i < SlotCount; i++)
        {
            SavedColors.Add(Color.White);
        }
    }

    /// <summary>
    /// Функция установки цвета из позиции в квадрате
    /// </summary>
    /// <param name="h"></param>
    /// <param name="s"></param>
    /// <param name="v"></param>
    /// <param name="alpha"></param>
    public void SetPicker(float h, float s, float v, float alpha)
    {
        Hue = h;
        Saturation = s;
        Brightness = v;
        Opacity = alpha;
        Color = Color.FromHsv(new Vector4(h, s, v, alpha));
        SavedColors[SelectedSlot] = Color;
    }

    /// <summary>
    /// Вшеняя функция установки цвета
    /// </summary>
    /// <param name="color"></param>
    public void SetExternal(Color color)
    {
        Color = color;
        Opacity = color.A;
        SavedColors[SelectedSlot] = color;

        var hsv = Color.ToHsv(color);
        if (hsv.Y > 0.02f && hsv.Z > 0.02f)
        {
            Hue = hsv.X;
            Saturation = hsv.Y;
            Brightness = hsv.Z;
        }
        else
        {
            Saturation = 0f;
            Brightness = hsv.Z;
        }
    }
}
