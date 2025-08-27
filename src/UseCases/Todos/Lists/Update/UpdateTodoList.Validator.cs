using UseCases.Todos.Lists.Common;

using FluentValidation;

namespace UseCases.Todos.Lists.Update;

public partial class UpdateTodoList
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new TodoListParamValidator());
        }
    }
}
