using Domain.schedule;

namespace Application.Common.DTO.Lessons;

public class LessonDTO
{
    public Guid Id { get; init; }
    public Guid ClassroomId { get; init; }
    public Guid TimeSlotId { get; init; }
    public Guid StreamId { get; init; }
    public ScheduleVersion Version { get; init; }
}
