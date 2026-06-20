namespace Domain.constraints;

/// <summary>
/// Градация желательности слота (день+пара) для преподавателя или аудитории.
/// Хранится в виде знакового штрафа <see cref="ClassroomAvailability.Penalty"/>/<see cref="TeacherAvailability.Penalty"/>:
/// отрицательный вес — бонус (слот предпочтителен), положительный — штраф (слот нежелателен).
/// Целевая функция солвера минимизируется, поэтому минимизация суммы штрафов выбирает желательные слоты.
/// </summary>
public enum AvailabilityState
{
    /// <summary>Слот обязателен — сильный бонус за назначение.</summary>
    Required = 0,
    /// <summary>Слот предпочтителен — небольшой бонус.</summary>
    Preferred = 1,
    /// <summary>Нейтральный слот — штраф не применяется (запись не хранится).</summary>
    Neutral = 2,
    /// <summary>Слот нежелателен — небольшой штраф.</summary>
    Discouraged = 3,
    /// <summary>Слот запрещён — большой штраф (мягкий запрет, чтобы не делать модель неразрешимой).</summary>
    Prohibited = 4,
}

/// <summary>Соответствие между градацией доступности и знаковым штрафом, хранимым в БД.</summary>
public static class AvailabilityStates
{
    /// <summary>Бонус за назначение в обязательный слот.</summary>
    public const int RequiredPenalty = -100;
    /// <summary>Бонус за назначение в предпочтительный слот.</summary>
    public const int PreferredPenalty = -10;
    /// <summary>Штраф за назначение в нежелательный слот.</summary>
    public const int DiscouragedPenalty = 10;
    /// <summary>Штраф за назначение в запрещённый слот.</summary>
    public const int ProhibitedPenalty = 100;

    /// <summary>Знаковый штраф для градации (Neutral → 0; такие записи не хранятся).</summary>
    public static int ToPenalty(AvailabilityState state) => state switch
    {
        AvailabilityState.Required => RequiredPenalty,
        AvailabilityState.Preferred => PreferredPenalty,
        AvailabilityState.Discouraged => DiscouragedPenalty,
        AvailabilityState.Prohibited => ProhibitedPenalty,
        _ => 0,
    };

    /// <summary>Восстановить градацию по хранимому штрафу (обратное к <see cref="ToPenalty"/>).</summary>
    public static AvailabilityState FromPenalty(int penalty) => penalty switch
    {
        <= RequiredPenalty => AvailabilityState.Required,
        < 0 => AvailabilityState.Preferred,
        0 => AvailabilityState.Neutral,
        >= ProhibitedPenalty => AvailabilityState.Prohibited,
        _ => AvailabilityState.Discouraged,
    };
}
