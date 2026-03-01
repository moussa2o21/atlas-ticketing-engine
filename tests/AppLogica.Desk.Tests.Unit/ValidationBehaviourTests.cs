using AppLogica.Desk.Application.Incidents.Commands.CreateIncident;
using AppLogica.Desk.Domain.Incidents;
using FluentAssertions;

namespace AppLogica.Desk.Tests.Unit;

/// <summary>
/// Tests for <see cref="CreateIncidentCommandValidator"/> verifying that
/// required fields are properly validated before reaching the handler.
/// </summary>
public class ValidationBehaviourTests
{
    private readonly CreateIncidentCommandValidator _validator = new();

    [Fact]
    public async Task ValidationBehaviour_Rejects_EmptyTitle()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            "",
            "Valid description",
            Impact.Department,
            Urgency.Normal,
            null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task ValidationBehaviour_Rejects_MissingDescription()
    {
        // Arrange
        var command = new CreateIncidentCommand(
            "Valid Title",
            "",
            Impact.Enterprise,
            Urgency.High,
            null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
