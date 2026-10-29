namespace Content.Shared._Lust.Implants;

/// <summary>
/// Симптомы оторжения имплантов. Каждый соответствует одному
/// статус-эффекту или действию (рвота, кровотечение).
/// </summary>
public enum Symptom : byte
{
    Drunk,      // головокружение
    Jitter,     // дрожь
    Stutter,    // спутанная речь
    Blurry,     // размытое зрение
    Vomit,      // рвота
    Bleed,      // кровотечение
}
