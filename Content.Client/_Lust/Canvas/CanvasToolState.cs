namespace Content.Client._Lust.Canvas;

public enum CanvasTool : byte { Pen, Brush, Fill, Eraser, Eyedropper }

public sealed class CanvasToolState
{
    public CanvasTool Tool { get; set; } = CanvasTool.Pen;
    public Color Color { get; set; } = Color.Black;
    public int Size { get; set; } = 2;
    public float Opacity { get; set; } = 1f;

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
}
