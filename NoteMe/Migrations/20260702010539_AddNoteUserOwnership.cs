using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteMe.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Notes",
                type: "int",
                nullable: true);

            // Preserve notes created before login support existed. If the database
            // has no user yet, create the same default admin account used by the app.
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Notes]) AND NOT EXISTS (SELECT 1 FROM [Users])
                BEGIN
                    INSERT INTO [Users] ([Username], [PasswordHash], [PasswordSalt], [CreatedAt])
                    VALUES ('admin', 'U5C3DoOe7iSlIENqaaX8v919YEgotfT4081NfqCFv10=',
                            'Tm90ZU1lRGVmYXVsdFNhbHQ=', GETDATE());
                END;

                UPDATE [Notes]
                SET [UserId] = (SELECT MIN([Id]) FROM [Users])
                WHERE [UserId] IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Notes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_UserId",
                table: "Notes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_UserId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_UserId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Notes");
        }
    }
}
