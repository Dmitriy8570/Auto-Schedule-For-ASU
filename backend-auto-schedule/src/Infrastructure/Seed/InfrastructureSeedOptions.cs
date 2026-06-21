namespace Infrastructure.Seed;

/// <summary>Настройки сидера инфраструктуры (секция конфигурации "InfrastructureSeed").</summary>
public sealed class InfrastructureSeedOptions
{
    public const string SectionName = "InfrastructureSeed";

    /// <summary>Включён ли сидер. Если выключен — служба бездействует.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Наполнять ли аудиторный фонд при старте (корпуса + аудитории).</summary>
    public bool SeedFacilities { get; set; } = true;

    /// <summary>Достраивать ли календарную сетку (рабочие дни + пары) для недель семестра.</summary>
    public bool SeedCalendarGrid { get; set; } = true;

    /// <summary>Число рабочих дней в неделе, начиная с понедельника (по умолчанию 6 — Пн–Сб).</summary>
    public int WorkingDays { get; set; } = 6;

    /// <summary>Число пар в учебный день (по умолчанию 8).</summary>
    public int PairsPerDay { get; set; } = 8;

    /// <summary>
    /// Корпуса и аудитории для наполнения. Если не задано — используется встроенный набор
    /// <see cref="DefaultBuildings"/> (3 корпуса, по этажам, разной вместимости).
    /// </summary>
    public BuildingSeed[]? Buildings { get; set; }

    /// <summary>Встроенный набор корпусов: А/Б/В, по 5 этажей × 5 аудиторий, вместимость по этажу.</summary>
    public static BuildingSeed[] DefaultBuildings()
    {
        var names = new[] { "А", "Б", "В" };
        // Вместимость растёт от верхних этажей к нижним: 1-й — поточные, выше — семинарские.
        var capacityByFloor = new[] { 120, 90, 60, 45, 30 };

        return names.Select(building =>
        {
            var classrooms = new List<ClassroomSeed>();
            for (int floor = 1; floor <= capacityByFloor.Length; floor++)
                for (int room = 1; room <= 5; room++)
                    classrooms.Add(new ClassroomSeed
                    {
                        Name = $"{building}-{floor}{room:00}",
                        Capacity = capacityByFloor[floor - 1]
                    });

            return new BuildingSeed { Name = $"Корпус {building}", Classrooms = classrooms.ToArray() };
        }).ToArray();
    }
}

/// <summary>Описание корпуса для сидинга.</summary>
public sealed class BuildingSeed
{
    public string Name { get; set; } = string.Empty;
    public ClassroomSeed[] Classrooms { get; set; } = [];
}

/// <summary>Описание аудитории для сидинга.</summary>
public sealed class ClassroomSeed
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
