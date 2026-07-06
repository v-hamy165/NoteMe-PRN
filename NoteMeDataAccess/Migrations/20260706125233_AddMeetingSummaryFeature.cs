using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteMe.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingSummaryFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoteId = table.Column<int>(type: "int", nullable: false),
                    AudioFileName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Transcript = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MainContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedSteps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextSteps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingSummaries_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSummaries_NoteId",
                table: "MeetingSummaries",
                column: "NoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingSummaries");
        }
    }
}
