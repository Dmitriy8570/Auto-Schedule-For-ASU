using Domain.constraints;
using Domain.constraints.equipments;
using Domain.schedule;

namespace Domain.university.buildings
{
    /// <summary>Учебная аудитория с информацией о вместимости и оснащении.</summary>
    public class Classroom
    {
        private Classroom() { }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <summary>Максимальная вместимость аудитории (количество мест).</summary>
        public int Capacity { get; private set; }

        public Guid BuildingId { get; private set; }
        public Building Building { get; private set; }

        /// <summary>Занятия, назначенные в данную аудиторию.</summary>
        public List<Lesson> Lessons { get; private set; }

        /// <summary>Ограничения доступности аудитории по дням и парам.</summary>
        public List<ClassroomAvailability> ClassroomAvailabilities { get; private set; }

        /// <summary>Оборудование, установленное в аудитории.</summary>
        public List<EquipmentRoom> EquipmentRooms { get; private set; }
    }
}
