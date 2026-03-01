using FluentValidation;

namespace AppLogica.Desk.Application.Incidents.Commands.AssignIncident;

/// <summary>
/// Validates <see cref="AssignIncidentCommand"/> before it reaches the handler.
/// </summary>
public sealed class AssignIncidentCommandValidator : AbstractValidator<AssignIncidentCommand>
{
    public AssignIncidentCommandValidator()
    {
        RuleFor(x => x.IncidentId)
            .NotEmpty();

        RuleFor(x => x.AssigneeId)
            .NotEmpty();
    }
}
