namespace Content.Shared._Lust.Implants;

/// <summary>
/// Симптомы оторжения имплантов. Каждый соответствует одному
/// статус-эффекту или действию (рвота, кровотечение).
/// </summary>
public enum Symptom : byte
{
    Jitter,     // дрожь
    Stutter,    // спутанная речь
    Vomit,      // рвота
    Bleed,      // кровотечение
    Woozy,
}
