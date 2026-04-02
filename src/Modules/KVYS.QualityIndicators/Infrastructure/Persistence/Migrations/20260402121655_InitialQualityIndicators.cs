using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KVYS.QualityIndicators.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialQualityIndicators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "quality");

            migrationBuilder.CreateTable(
                name: "IndicatorCategories",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicatorCategories_IndicatorCategories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "quality",
                        principalTable: "IndicatorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Formula = table.Column<string>(type: "text", nullable: true),
                    TargetValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    TargetOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CollectionFrequency = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Indicators_IndicatorCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "quality",
                        principalTable: "IndicatorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicatorEntries",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Semester = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric(15,4)", precision: 15, scale: 4, nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicatorEntries_Indicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalSchema: "quality",
                        principalTable: "Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceDocuments",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_IndicatorEntries_IndicatorEntryId",
                        column: x => x.IndicatorEntryId,
                        principalSchema: "quality",
                        principalTable: "IndicatorEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_IndicatorEntryId",
                schema: "quality",
                table: "EvidenceDocuments",
                column: "IndicatorEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorCategories_Code",
                schema: "quality",
                table: "IndicatorCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorCategories_ParentId",
                schema: "quality",
                table: "IndicatorCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorEntries_IndicatorId_UnitId_AcademicYear_Semester",
                schema: "quality",
                table: "IndicatorEntries",
                columns: new[] { "IndicatorId", "UnitId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_CategoryId",
                schema: "quality",
                table: "Indicators",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Code",
                schema: "quality",
                table: "Indicators",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenceDocuments",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "IndicatorEntries",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "Indicators",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "IndicatorCategories",
                schema: "quality");
        }
    }
}
