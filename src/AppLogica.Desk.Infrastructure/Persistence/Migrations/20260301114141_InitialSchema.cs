using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppLogica.Desk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "desk");

            migrationBuilder.CreateTable(
                name: "incidents",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Impact = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Urgency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IncidentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueueId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsMajorIncident = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sla_policies",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sla_timers",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolutionDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PauseReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ElapsedBeforePause = table.Column<TimeSpan>(type: "interval", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_timers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sla_policy_targets",
                schema: "desk",
                columns: table => new
                {
                    SlaPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResponseMinutes = table.Column<int>(type: "integer", nullable: false),
                    ResolutionMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_policy_targets", x => new { x.SlaPolicyId, x.Id });
                    table.ForeignKey(
                        name: "FK_sla_policy_targets_sla_policies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalSchema: "desk",
                        principalTable: "sla_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_tenant_assignee",
                schema: "desk",
                table: "incidents",
                columns: new[] { "TenantId", "AssigneeId" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_tenant_created_desc",
                schema: "desk",
                table: "incidents",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_tenant_status",
                schema: "desk",
                table: "incidents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_ticket_number",
                schema: "desk",
                table: "incidents",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sla_timers_tenant_incident",
                schema: "desk",
                table: "sla_timers",
                columns: new[] { "TenantId", "IncidentId" });

            migrationBuilder.CreateIndex(
                name: "ix_sla_timers_tenant_status_resolution_due",
                schema: "desk",
                table: "sla_timers",
                columns: new[] { "TenantId", "Status", "ResolutionDueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incidents",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "sla_policy_targets",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "sla_timers",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "sla_policies",
                schema: "desk");
        }
    }
}
