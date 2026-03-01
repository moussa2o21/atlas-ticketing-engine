using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppLogica.Desk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_workflows",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TimeoutMinutes = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_approval_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_catalog_categories",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_service_catalog_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_catalog_categories_service_catalog_categories_Paren~",
                        column: x => x.ParentCategoryId,
                        principalSchema: "desk",
                        principalTable: "service_catalog_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approval_workflow_steps",
                schema: "desk",
                columns: table => new
                {
                    StepOrder = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApprovalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_workflow_steps", x => new { x.ApprovalWorkflowId, x.StepOrder });
                    table.ForeignKey(
                        name: "FK_approval_workflow_steps_approval_workflows_ApprovalWorkflow~",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "desk",
                        principalTable: "approval_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_catalog_items",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfillmentInstructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ExpectedDeliveryMinutes = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_service_catalog_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_catalog_items_approval_workflows_ApprovalWorkflowId",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "desk",
                        principalTable: "approval_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_catalog_items_service_catalog_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "desk",
                        principalTable: "service_catalog_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CatalogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FulfillmentNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_service_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_requests_service_catalog_items_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalSchema: "desk",
                        principalTable: "service_catalog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_tasks",
                schema: "desk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_fulfillment_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fulfillment_tasks_service_requests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "desk",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approval_workflows_tenant",
                schema: "desk",
                table: "approval_workflows",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_tasks_ServiceRequestId",
                schema: "desk",
                table: "fulfillment_tasks",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_tasks_tenant_request",
                schema: "desk",
                table: "fulfillment_tasks",
                columns: new[] { "TenantId", "ServiceRequestId" });

            migrationBuilder.CreateIndex(
                name: "ix_fulfillment_tasks_tenant_status",
                schema: "desk",
                table: "fulfillment_tasks",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_categories_ParentCategoryId",
                schema: "desk",
                table: "service_catalog_categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_service_catalog_categories_tenant_parent",
                schema: "desk",
                table: "service_catalog_categories",
                columns: new[] { "TenantId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "ix_service_catalog_categories_tenant_sort",
                schema: "desk",
                table: "service_catalog_categories",
                columns: new[] { "TenantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_items_ApprovalWorkflowId",
                schema: "desk",
                table: "service_catalog_items",
                column: "ApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_items_CategoryId",
                schema: "desk",
                table: "service_catalog_items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_service_catalog_items_tenant_category",
                schema: "desk",
                table: "service_catalog_items",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "ix_service_catalog_items_tenant_sort",
                schema: "desk",
                table: "service_catalog_items",
                columns: new[] { "TenantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_CatalogItemId",
                schema: "desk",
                table: "service_requests",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_created_desc",
                schema: "desk",
                table: "service_requests",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_request_number",
                schema: "desk",
                table: "service_requests",
                columns: new[] { "TenantId", "RequestNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_requester",
                schema: "desk",
                table: "service_requests",
                columns: new[] { "TenantId", "RequesterId" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_status",
                schema: "desk",
                table: "service_requests",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_workflow_steps",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "fulfillment_tasks",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "service_requests",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "service_catalog_items",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "approval_workflows",
                schema: "desk");

            migrationBuilder.DropTable(
                name: "service_catalog_categories",
                schema: "desk");
        }
    }
}
