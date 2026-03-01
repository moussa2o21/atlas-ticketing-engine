using FluentValidation;

namespace AppLogica.Desk.Application.Incidents.Commands.CreateIncident;

/// <summary>
/// Validates <see cref="CreateIncidentCommand"/> before it reaches the handler.
/// </summary>
public sealed class CreateIncidentCommandValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.Impact)
            .IsInEnum();

        RuleFor(x => x.Urgency)
            .IsInEnum();
    }
}
