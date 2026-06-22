using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Common.Lessons.Commands;

/// <summary>
/// Изменить ранее созданное занятие: сменить аудиторию, временной слот и/или учебный план
/// (дисциплина/преподаватель/тип) одной командой. Поток берётся из выбранного плана.
/// </summary>
public class UpdateLessonCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid ClassroomId { get; init; }
    public Guid TimeSlotId { get; init; }
    public Guid StreamId { get; init; }

    /// <summary>Учебный план, который реализует занятие. Необязателен.</summary>
    public Guid? CurriculumId { get; init; }
}

public class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ITransactionRunner _transactionRunner;

    public UpdateLessonCommandHandler(ILessonRepository lessonRepository, ITransactionRunner transactionRunner)
    {
        _lessonRepository = lessonRepository;
        _transactionRunner = transactionRunner;
    }

    public async Task Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        // Проверка коллизий по НОВЫМ параметрам (с исключением самого занятия) и запись —
        // в одной SERIALIZABLE-транзакции, как при создании: одновременные правки не могут
        // «проскочить» проверку (TOCTOU), конфликтующая транзакция будет повторена СУБД.
        await _transactionRunner.ExecuteSerializableAsync(async ct =>
        {
            var lesson = await _lessonRepository.GetTrackedByIdAsync(request.Id, ct)
                ?? throw new KeyNotFoundException();

            var conflicts = await _lessonRepository.FindConflictsAsync(
                request.ClassroomId, request.TimeSlotId, request.StreamId, request.CurriculumId,
                excludeLessonId: request.Id, ct);
            if (conflicts.Count > 0)
                throw new ScheduleConflictException(conflicts);

            lesson.Reschedule(request.ClassroomId, request.TimeSlotId, request.StreamId, request.CurriculumId);
            await _lessonRepository.SaveChangesAsync(ct);
        }, cancellationToken);
    }
}
