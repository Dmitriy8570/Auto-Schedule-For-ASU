using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.schedule;
using MediatR;

namespace Application.Common.Lessons.Commands;

public class CreateLessonCommand : IRequest<Guid>
{
    public Guid ClassroomId { get; init; }
    public Guid TimeSlotId { get; init; }
    public Guid StreamId { get; init; }
    public Guid SemesterId { get; init; }

    /// <summary>Учебный план (дисциплина/преподаватель/тип), который реализует занятие. Необязателен.</summary>
    public Guid? CurriculumId { get; init; }
}

public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Guid>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ITransactionRunner _transactionRunner;

    public CreateLessonCommandHandler(ILessonRepository lessonRepository, ITransactionRunner transactionRunner)
    {
        _lessonRepository = lessonRepository;
        _transactionRunner = transactionRunner;
    }

    public async Task<Guid> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        var lessonId = Guid.NewGuid();

        // Ручное добавление обходит солвер, поэтому жёсткие ограничения (пересечения по
        // аудитории/преподавателю/группе в одном слоте) проверяются здесь. Проверка и вставка
        // выполняются в одной SERIALIZABLE-транзакции: при одновременном добавлении двух занятий
        // в один слот проверка не может «проскочить» (TOCTOU) — конфликтующая транзакция будет
        // отменена СУБД и автоматически повторена.
        await _transactionRunner.ExecuteSerializableAsync(async ct =>
        {
            var conflicts = await _lessonRepository.FindConflictsAsync(
                request.ClassroomId, request.TimeSlotId, request.StreamId, request.CurriculumId, null, ct);
            if (conflicts.Count > 0)
                throw new ScheduleConflictException(conflicts);

            var lesson = Lesson.Create(
                lessonId, request.ClassroomId, request.TimeSlotId, request.StreamId, request.SemesterId, request.CurriculumId);
            await _lessonRepository.AddAsync(lesson, ct);
            await _lessonRepository.SaveChangesAsync(ct);
        }, cancellationToken);

        return lessonId;
    }
}
