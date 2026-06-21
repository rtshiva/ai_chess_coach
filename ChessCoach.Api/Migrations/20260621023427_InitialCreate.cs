using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCoach.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MoveAnalysisEvents",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlySequenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    FenBefore = table.Column<string>(type: "TEXT", nullable: false),
                    UserMoveUci = table.Column<string>(type: "TEXT", nullable: false),
                    CentipawnLoss = table.Column<int>(type: "INTEGER", nullable: false),
                    StructuralQuality = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryCategory = table.Column<string>(type: "TEXT", nullable: false),
                    SubCategory = table.Column<string>(type: "TEXT", nullable: false),
                    TacticalEngineVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveAnalysisEvents", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_analytics_lookup",
                table: "MoveAnalysisEvents",
                columns: new[] { "UserId", "PrimaryCategory", "SubCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_MoveAnalysisEvents_GameId_PlySequenceId",
                table: "MoveAnalysisEvents",
                columns: new[] { "GameId", "PlySequenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoveAnalysisEvents");
        }
    }
}
