using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CF.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeIdFromDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Device_DeviceType_DeviceTypeId",
                table: "Device");

            migrationBuilder.DropForeignKey(
                name: "FK_Device_Employee_EmployeeId",
                table: "Device");

            migrationBuilder.DropIndex(
                name: "IX_Device_EmployeeId",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Device");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceTypeId",
                table: "Device",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Device_DeviceType_DeviceTypeId",
                table: "Device",
                column: "DeviceTypeId",
                principalTable: "DeviceType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Device_DeviceType_DeviceTypeId",
                table: "Device");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceTypeId",
                table: "Device",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Device",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Device_EmployeeId",
                table: "Device",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Device_DeviceType_DeviceTypeId",
                table: "Device",
                column: "DeviceTypeId",
                principalTable: "DeviceType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Device_Employee_EmployeeId",
                table: "Device",
                column: "EmployeeId",
                principalTable: "Employee",
                principalColumn: "Id");
        }
    }
}
