using Domain.university.groups;

namespace Domain.schedule
{
    /// <summary>Связующая таблица между потоком и учебной группой (многие-ко-многим).</summary>
    public class StreamGroups
    {
        private StreamGroups() { }

        public Guid GroupId { get; private set; }
        public Guid StreamId { get; private set; }

        public Group Group { get; private set; }
        public AcademicStream Stream { get; private set; }
    }
}
