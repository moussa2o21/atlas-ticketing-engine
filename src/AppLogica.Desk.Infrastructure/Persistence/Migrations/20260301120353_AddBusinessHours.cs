using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppLogica.Desk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_hours_calendars",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Profile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DayStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DayEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    WorkingDays = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_business_hours_calendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "public_holidays",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CalendarId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_public_holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_public_holidays_business_hours_calendars_CalendarId",
                        column: x => x.CalendarId,
                        principalSchema: "desk",
                        principalTable: "business_hours_calendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_business_hours_calendars_tenant",
                schema: "desk",
                table: "business_hours_calendars",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_business_hours_calendars_tenant_default",
                schema: "desk",
                table: "business_hours_calendars",
                columns: new[] { "TenantId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_public_holidays_calendar_date",
                schema: "desk",
                table: "public_holidays",
                columns: new[] { "CalendarId", "Date" });

            migrationBuilder.CreateIndex(
                name: "ix_public_holidays_tenant",
                schema: "desk",
                table: "public_holidays",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "public_holidays",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "business_hours_calendars",
                schema: "desk");
        }
    }
}
