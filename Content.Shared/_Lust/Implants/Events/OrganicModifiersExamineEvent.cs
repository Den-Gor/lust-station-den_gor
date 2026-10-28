using Robust.Shared.Utility;

namespace Content.Shared._Lust.Implants.Events;

/// <summary>
/// Поднимается на теле при осмотре. Системы-модификаторы добавляют
/// свои секции в Message. Координатор выводит итог в ExamineVerb.
/// </summary>
[ByRefEvent]
public record struct BodyModifiersExamineEvent(FormattedMessage Message);

/// <summary>
/// Поднимается на органе при осмотре. Системы-модификаторы добавляют
/// свои секции в Message. Координатор выводит итог в ExamineVerb.
/// </summary>
[ByRefEvent]
public record struct OrganModifiersExamineEvent(FormattedMessage Message);
