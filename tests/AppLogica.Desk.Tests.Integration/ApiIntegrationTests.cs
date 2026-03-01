using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppLogica.Desk.Tests.Integration;

/// <summary>
/// Integration tests that exercise the API endpoints via <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// Tests health check endpoints (anonymous) and verifies that incident endpoints require authentication.
/// </summary>
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Provide required configuration values so the app can start
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DB:ConnectionString"] = "Host=localhost;Database=desk_test;Username=test;Password=test",
                    ["Auth:Authority"] = "https://localhost",
                    ["Auth:Audience"] = "desk-api"
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            // Do not follow redirects so we can inspect 401/403 responses
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task HealthCheck_Live_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_Ready_Returns200Or503()
    {
        // Act: Without a real database, readiness may return 503, and that is acceptable
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task CreateIncident_WithoutAuth_Returns401()
    {
        // Arrange
        var content = new StringContent(
            """{"title":"Test","description":"Test desc","impact":0,"urgency":0}""",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/desk/incidents", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListIncidents_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/desk/incidents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetIncident_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync($"/api/desk/incidents/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
