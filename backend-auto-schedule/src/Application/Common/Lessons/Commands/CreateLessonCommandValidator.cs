using FluentValidation;

namespace Application.Common.Lessons.Commands;

public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.ClassroomId).NotEmpty().WithMessage("ClassroomId is required.");
        RuleFor(x => x.TimeSlotId).NotEmpty().WithMessage("TimeSlotId is required.");
        RuleFor(x => x.StreamId).NotEmpty().WithMessage("StreamId is required.");
        RuleFor(x => x.SemesterId).NotEmpty().WithMessage("SemesterId is required.");
    }
}