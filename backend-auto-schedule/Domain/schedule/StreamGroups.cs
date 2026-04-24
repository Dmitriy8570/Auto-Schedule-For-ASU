using Domain.university.groups;

namespace Domain.schedule
{
    /// <summary>Связующая таблица между потоком и учебной группой (многие-ко-многим).</summary>
    public class StreamGroups
    {
        public Guid GroupId { get; set; }
        public Guid StreamId { get; set; }

        public Group Group { get; set; }
        public AcademicStream Stream { get; set; }
    }
}
