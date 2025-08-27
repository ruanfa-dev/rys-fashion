using FluentValidation;

using static Core.Todos.TodoItem.Constraints;
using static Core.Todos.TodoItem.Errors;

namespace UseCases.Todos.Items.Common;

public class TodoItemParamValidator : AbstractValidator<TodoItemParam>
{
    public TodoItemParamValidator()
    {

        RuleFor(v => v.Title)
            .MinimumLength(MinTitleLength)
            .WithErrorCode(TitleTooShort.Code)
            .WithMessage(TitleTooLong.Description)
            .MaximumLength(MaxTitleLength)
            .WithErrorCode(TitleTooLong.Code)
            .WithMessage(TitleTooLong.Description)
            .Matches(TitleAllowedPattern)
            .WithErrorCode(TitleInvalidFormat.Code)
            .WithMessage(TitleInvalidFormat.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(v => v.Note)
            .MaximumLength(MaxNoteLength)
            .WithErrorCode(NoteTooLong.Code)
            .WithMessage( NoteTooLong.Description)
            .When(v => !string.IsNullOrEmpty(v.Note));

        RuleFor(v => v.Priority)
            .IsInEnum()
            .WithErrorCode(PriorityLevelInvalid.Code)
            .WithMessage(PriorityLevelInvalid.Description)
            .When(v => v.Priority.HasValue);

        RuleFor(v => v.Reminder)
            .GreaterThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithErrorCode(ReminderMustBeInFuture.Code)
            .WithMessage(ReminderMustBeInFuture.Description)
            .When(v => v.Reminder.HasValue);
    }
}
