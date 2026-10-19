using System.Numerics;
using Content.Client._Lust.ColorPicker;

namespace Content.Client._Lust.Canvas;

public enum CanvasTool : byte {Pen, Brush, Fill, Eraser, Eyedropper}

/// <summary>
/// Тулбокс настроект инстрементов холста, реализует загрузку хранение и изменение настроект, таких как тип инструмента,
/// цвет в ечейках палитры, размер кисти, прозрачность,
/// </summary>
public sealed class CanvasToolState
{
    public CanvasTool Tool { get; set; } = CanvasTool.Pen;

    public int Size { get; set; } = 2;

    public const int SlotCount = 12;

    public List<ColorSlot> Palette { get; } = new(SlotCount);

    public int SelectedIndex { get; private set; }

    /// <summary>
    /// Цвет выбранной ячейки. Запись идёт в эту же ячейку.
    /// </summary>
    public Color Color => Palette[SelectedIndex].Color;

    /// <summary>
    /// Получение Hsv выбраной  ячейки
    /// </summary>
    public Vector4 SelectedHsv => Palette[SelectedIndex].Hsv;

    /// <summary>
    /// Прозрачность кисти. Пишется только через <see cref="SetOpacity"/>, зовёт ползунок
    /// </summary>
    public float Opacity { get; private set; } = 1f;

    /// <summary>
    /// Цвет текущий палитры на который накладывается прозрачность
    /// </summary>
    public Color BrushColor => Color with { A = Opacity };

    /// <summary>
    /// Сработало при любом изменении цвета или прозрачности
    /// </summary>
    public event Action? Changed;

    public CanvasToolState()
    {
        Palette.Add(new ColorSlot(Color.Black));
        for (var i = 1; i < SlotCount; i++)
        {
            Palette.Add(new ColorSlot(Color.White));
        }
    }

    /// <summary>
    /// Установка прозрачности
    /// </summary>
    public void SetOpacity(float opacity)
    {
        Opacity = Math.Clamp(opacity, 0f, 1f);
        Changed?.Invoke();
    }

    /// <summary>
    /// Выбрать ячейку палитры.
    /// </summary>
    public void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= Palette.Count)
            return;

        SelectedIndex = slot;
        Changed?.Invoke();
    }

    /// <summary>
    /// Изменине Hsv
    /// </summary>
    public void SetHsvSelection(float h, float s, float v)
    {
        Palette[SelectedIndex].SetHsv(h, s, v);
        Changed?.Invoke();
    }

    /// <summary>
    /// Внешняя установка цвета
    /// </summary>
    public void SetExternal(Color color)
    {
        Palette[SelectedIndex].SetExternal(color.WithAlpha(1f));
        Changed?.Invoke();
    }
}
