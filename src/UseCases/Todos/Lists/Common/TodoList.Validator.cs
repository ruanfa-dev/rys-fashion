using FluentValidation;

using static Core.Todos.TodoList.Constraints;
using static Core.Todos.TodoList.Errors;

namespace UseCases.Todos.Lists.Common;

public class TodoListParamValidator : AbstractValidator<TodoListParam>
{
    public TodoListParamValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithErrorCode(TitleRequired.Code)
            .WithMessage(TitleRequired.Description)
            .MaximumLength(MaxTitleLength)
            .WithErrorCode(TitleTooLong.Code)
            .WithMessage(TitleTooLong.Description)
            .MinimumLength(MinTitleLength)
            .WithErrorCode(TitleTooShort.Code)
            .WithMessage(TitleTooShort.Description)
            .Matches(TitleAllowedPattern)
            .WithErrorCode(TitleInvalidFormat.Code)
            .WithMessage(TitleInvalidFormat.Description);

        RuleFor(x => x.Colour)
            .NotEmpty()
            .WithErrorCode(ColourRequired.Code)
            .WithMessage(ColourRequired.Description)
            .Matches(ColourRegex)
            .WithErrorCode(ColourInvalidFormat.Code)
            .WithMessage(ColourInvalidFormat.Description);
    }
}
