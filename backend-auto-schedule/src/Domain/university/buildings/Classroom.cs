using Domain.constraints;
using Domain.constraints.equipments;
using Domain.schedule;

namespace Domain.university.buildings
{
    /// <summary>Учебная аудитория с информацией о вместимости и оснащении.</summary>
    public class Classroom
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        /// <summary>Максимальная вместимость аудитории (количество мест).</summary>
        public int Capacity { get; set; }

        public Guid BuildingId { get; set; }
        public Building Building { get; set; }

        /// <summary>Занятия, назначенные в данную аудиторию.</summary>
        public List<Lesson> Lessons { get; set; }

        /// <summary>Ограничения доступности аудитории по дням и парам.</summary>
        public List<ClassroomAvailability> ClassroomAvailabilities { get; set; }

        /// <summary>Оборудование, установленное в аудитории.</summary>
        public List<EquipmentRoom> EquipmentRooms { get; set; }
    }
}
