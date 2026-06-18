namespace Domain.common;

/// <summary>Проверки инвариантов доменных сущностей: бросают исключение при нарушении и возвращают проверенное значение.</summary>
public static class Guard
{
    /// <summary>Идентификатор не должен быть пустым.</summary>
    public static Guid NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{name} не может быть пустым.", name);
        return value;
    }

    /// <summary>Строка не должна быть пустой или состоять из одних пробелов; возвращает обрезанное значение.</summary>
    public static string NotBlank(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} не может быть пустым.", name);
        return value.Trim();
    }

    /// <summary>Число должно быть положительным (&gt; 0).</summary>
    public static int Positive(int value, string name)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(name, value, $"{name} должно быть больше нуля.");
        return value;
    }

    /// <summary>Число не должно быть отрицательным (&gt;= 0).</summary>
    public static int NotNegative(int value, string name)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(name, value, $"{name} не может быть отрицательным.");
        return value;
    }

    /// <summary>Значение перечисления должно быть определено в типе.</summary>
    public static TEnum Defined<TEnum>(TEnum value, string name) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(name, value, $"Недопустимое значение {name}.");
        return value;
    }
}
