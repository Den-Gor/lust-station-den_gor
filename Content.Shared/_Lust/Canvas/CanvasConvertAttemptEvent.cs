namespace Content.Shared._Lust.Canvas;

/// <summary>
/// Хук: кистью ткнули в бумагу. CanvasSystem может забрать обработку (конвертация в холст).
/// </summary>
[ByRefEvent]
public record struct CanvasConvertAttemptEvent(EntityUid User, EntityUid Brush, bool Cancelled = false);
