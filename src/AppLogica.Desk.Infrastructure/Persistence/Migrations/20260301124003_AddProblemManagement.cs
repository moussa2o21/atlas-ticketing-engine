using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppLogica.Desk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "problems",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Impact = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCause = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Workaround = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsKnownError = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    KnownErrorPublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    linked_incident_ids = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_problems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_problems_tenant_assignee",
                schema: "desk",
                table: "problems",
                columns: new[] { "TenantId", "AssigneeId" });

            migrationBuilder.CreateIndex(
                name: "ix_problems_tenant_created_desc",
                schema: "desk",
                table: "problems",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_problems_tenant_known_error",
                schema: "desk",
                table: "problems",
                columns: new[] { "TenantId", "IsKnownError" },
                filter: "\"IsKnownError\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_problems_tenant_problem_number",
                schema: "desk",
                table: "problems",
                columns: new[] { "TenantId", "ProblemNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_problems_tenant_status",
                schema: "desk",
                table: "problems",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "problems",
                schema: "desk");
        }
    }
}
