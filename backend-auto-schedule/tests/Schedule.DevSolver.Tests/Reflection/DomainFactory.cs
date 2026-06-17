using System.Reflection;

namespace Schedule.DevSolver.Tests.Reflection;

/// <summary>
/// Доменные сущности спроектированы как «анемичные» агрегаты с приватными конструкторами
/// и приватными сеттерами (их населяет EF Core через backing fields). В тестах настоящего
/// ORM нет, поэтому объекты создаются и наполняются через рефлексию. Это сознательный
/// компромисс: тест проверяет солвер, а не доменную инкапсуляцию.
/// </summary>
public static class DomainFactory
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>Создаёт экземпляр через приватный конструктор без параметров.</summary>
    public static T New<T>() => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    /// <summary>Устанавливает значение свойства, обращаясь к его приватному сеттеру.</summary>
    public static T Set<T>(this T target, string property, object? value)
    {
        var prop = typeof(T).GetProperty(property, Flags)
                   ?? throw new MissingMemberException(typeof(T).Name, property);

        var setter = prop.GetSetMethod(nonPublic: true)
                     ?? throw new InvalidOperationException($"{typeof(T).Name}.{property} не имеет сеттера.");

        setter.Invoke(target, new[] { value });
        return target;
    }
}
