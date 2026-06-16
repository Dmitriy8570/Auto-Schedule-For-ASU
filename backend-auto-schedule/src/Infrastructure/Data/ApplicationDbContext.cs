using Domain.calendar;
using Domain.constraints;
using Domain.constraints.equipments;
using Domain.constraints.penalty;
using Domain.schedule;
using Domain.university.buildings;
using Domain.university.common;
using Domain.university.groups;
using Domain.university.teachers;
using Domain.workload;
using Domain.workload.logs;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Расписание.
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<AcademicStream> AcademicStreams { get; set; }
    public DbSet<StreamGroups> StreamGroups { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }

    // Учебная нагрузка.
    public DbSet<Curriculum> Curriculums { get; set; }
    public DbSet<WeekWorkload> WeekWorkloads { get; set; }
    public DbSet<SemesterWorkload> SemesterWorkloads { get; set; }
    public DbSet<WeekLog> WeekLogs { get; set; }
    public DbSet<SemesterLog> SemesterLogs { get; set; }

    // Календарь.
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Week> Weeks { get; set; }
    public DbSet<WeekDay> WeekDays { get; set; }

    // Университет.
    public DbSet<Institute> Institutes { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Degree> Degrees { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }

    // Ограничения.
    public DbSet<ClassroomAvailability> ClassroomAvailabilities { get; set; }
    public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
    public DbSet<ConstraintConfig> ConstraintConfigs { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<EquipmentRoom> EquipmentRooms { get; set; }
    public DbSet<NeededEquipment> NeededEquipments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
