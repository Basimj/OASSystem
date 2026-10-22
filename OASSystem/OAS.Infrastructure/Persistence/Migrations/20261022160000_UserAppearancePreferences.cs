using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OAS.Infrastructure.Persistence.Migrations;

// Intentional small SQL document table: not registered as an EF entity. The
// EF snapshot and the runtime entity model therefore remain unchanged.
[DbContext(typeof(OAS.Infrastructure.Persistence.OasDbContext))]
[Migration("20261022160000_UserAppearancePreferences")]
public sealed class UserAppearancePreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserAppearancePreferences", schema: "security",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserAppearancePreferences", x => x.UserId);
                table.ForeignKey("FK_UserAppearancePreferences_Users", x => x.UserId,
                    principalSchema: "security", principalTable: "Users", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_UserAppearancePreferences_ValidJson", "ISJSON([SettingsJson]) = 1");
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "UserAppearancePreferences", schema: "security");
}
