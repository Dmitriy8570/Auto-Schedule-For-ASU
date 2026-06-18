using Domain.common;
using Domain.university.common;

namespace Domain.university.teachers;

/// <summary>Кафедра университета, объединяющая преподавателей по научно-методическому направлению.</summary>
public class Department
{
    private Department() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>Преподаватели, прикреплённые к кафедре.</summary>
    public List<Teacher> Teachers { get; private set; } = [];

    public Guid InstituteId { get; private set; }
    public Institute Institute { get; private set; } = null!;

    /// <summary>Создать кафедру в составе института.</summary>
    public static Department Create(Guid id, string name, Guid instituteId) => new()
    {
        Id = Guard.NotEmpty(id, nameof(id)),
        Name = Guard.NotBlank(name, nameof(name)),
        InstituteId = Guard.NotEmpty(instituteId, nameof(instituteId))
    };
}
