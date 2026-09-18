using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomRental.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalManagementFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhGiaPhong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    TenantAccountId = table.Column<int>(type: "int", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaPhong", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatCoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RentalRequestId = table.Column<int>(type: "int", nullable: false),
                    TenantAccountId = table.Column<int>(type: "int", nullable: false),
                    LandlordAccountId = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatCoc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HopDongThue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RentalRequestId = table.Column<int>(type: "int", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    TenantAccountId = table.Column<int>(type: "int", nullable: false),
                    LandlordAccountId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TienThueHangThang = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NguoiThueDaXacNhan = table.Column<bool>(type: "bit", nullable: false),
                    ChuTroDaXacNhan = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongThue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuCo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ReporterAccountId = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HuongXuLy = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuCo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauThuePhong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    TenantAccountId = table.Column<int>(type: "int", nullable: false),
                    LandlordAccountId = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauThuePhong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuCauThuePhong_TinDang_PostId",
                        column: x => x.PostId,
                        principalTable: "TinDang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaPhong_ContractId_TenantAccountId",
                table: "DanhGiaPhong",
                columns: new[] { "ContractId", "TenantAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaPhong_PostId",
                table: "DanhGiaPhong",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_DatCoc_RentalRequestId",
                table: "DatCoc",
                column: "RentalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongThue_TenantAccountId_LandlordAccountId",
                table: "HopDongThue",
                columns: new[] { "TenantAccountId", "LandlordAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SuCo_ContractId",
                table: "SuCo",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThuePhong_PostId",
                table: "YeuCauThuePhong",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThuePhong_TenantAccountId_PostId",
                table: "YeuCauThuePhong",
                columns: new[] { "TenantAccountId", "PostId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhGiaPhong");

            migrationBuilder.DropTable(
                name: "DatCoc");

            migrationBuilder.DropTable(
                name: "HopDongThue");

            migrationBuilder.DropTable(
                name: "SuCo");

            migrationBuilder.DropTable(
                name: "YeuCauThuePhong");
        }
    }
}
