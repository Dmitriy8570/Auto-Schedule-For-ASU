using FluentValidation;

namespace Application.Common.Lessons.Commands;

public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        RuleFor(x => x.ClassroomId).NotEmpty().WithMessage("ClassroomId is required.");
        RuleFor(x => x.TimeSlotId).NotEmpty().WithMessage("TimeSlotId is required.");
        RuleFor(x => x.StreamId).NotEmpty().WithMessage("StreamId is required.");
    }
}
