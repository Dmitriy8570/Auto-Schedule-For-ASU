using Application.Common.DTO.Constraints;
using FluentValidation;

namespace Application.Common.Constraints;

/// <summary>
/// Проверка ячейки сетки доступности: день недели 0..5 (Пн–Сб), номер пары 1..8,
/// градация — допустимое значение перечисления.
/// </summary>
public sealed class AvailabilityCellValidator : AbstractValidator<AvailabilityCellDto>
{
    public AvailabilityCellValidator()
    {
        RuleFor(c => c.DayOfWeek).InclusiveBetween(0, 5)
            .WithMessage("День недели должен быть в диапазоне 0..5 (Пн–Сб).");
        RuleFor(c => c.PairNumber).InclusiveBetween(1, 8)
            .WithMessage("Номер пары должен быть в диапазоне 1..8.");
        RuleFor(c => c.State).IsInEnum()
            .WithMessage("Недопустимая градация доступности.");
    }
}
