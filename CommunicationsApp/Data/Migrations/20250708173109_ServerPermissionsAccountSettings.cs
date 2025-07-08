using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunicationsApp.Migrations
{
    /// <inheritdoc />
    public partial class ServerPermissionsAccountSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreferredLocale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayServerMemberList = table.Column<bool>(type: "bit", nullable: false),
                    PreferredTheme = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerPermissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionType = table.Column<int>(type: "int", nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerRolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerRolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_ServerRolePermissions_ServerPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "ServerPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerRolePermissions_ServerRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ServerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServerRolePermissions_ServerPermissions_PermissionId",
                table: "ServerRolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerRolePermissions_ServerRoles_RoleId",
                table: "ServerRolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountSettings_AspNetUsers_UserId",
                table: "AccountSettings");
        }
    }
}
