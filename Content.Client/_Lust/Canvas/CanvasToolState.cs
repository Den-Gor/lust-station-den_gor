namespace Content.Client._Lust.Canvas;

public enum CanvasTool : byte { Pen, Brush, Fill, Eraser, Eyedropper }

public sealed class CanvasToolState
{
    public CanvasTool Tool = CanvasTool.Pen;
    public Color Color = Color.Black;
    public int Size = 2;
    public float Opacity = 1f;

    public event Action? Changed;
    public void Touch() => Changed?.Invoke();
}
