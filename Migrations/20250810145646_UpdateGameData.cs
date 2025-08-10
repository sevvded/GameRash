using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameRash.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGameData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "GameID",
                keyValue: 1,
                columns: new[] { "Description", "Price" },
                values: new object[] { "An amazing RPG adventure game with stunning graphics and immersive gameplay. Experience epic battles and explore mysterious worlds.", 299.99m });

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "GameID",
                keyValue: 2,
                columns: new[] { "Description", "Price" },
                values: new object[] { "Explore the vast universe in this space exploration game. Discover new planets, trade resources, and survive in the harsh environment of space.", 199.99m });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "GameID", "CoverImage", "Description", "DeveloperID", "Price", "Title" },
                values: new object[] { 3, "indir.jpeg", "An open-world action-adventure story set in Night City, a megalopolis obsessed with power, glamour and body modification. You play as V, a mercenary outlaw going after a one-of-a-kind implant that is the key to immortality.", 1, 599.99m, "Cyberpunk 2077" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "GameID",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "GameID",
                keyValue: 1,
                columns: new[] { "Description", "Price" },
                values: new object[] { "An amazing RPG adventure game", 0m });

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "GameID",
                keyValue: 2,
                columns: new[] { "Description", "Price" },
                values: new object[] { "Explore the vast universe", 0m });
        }
    }
}
