using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RoomRental.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleNullLandlordCitizenIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Posts_PostId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Users_UserId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_PostAmenities_Amenities_AmenityId",
                table: "PostAmenities");

            migrationBuilder.DropForeignKey(
                name: "FK_PostAmenities_Posts_PostId",
                table: "PostAmenities");

            migrationBuilder.DropForeignKey(
                name: "FK_PostImages_Posts_PostId",
                table: "PostImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Users_LandlordId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Posts_PostId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_ViewingAppointments_Posts_PostId",
                table: "ViewingAppointments");

            migrationBuilder.DropForeignKey(
                name: "FK_ViewingAppointments_Users_TenantId",
                table: "ViewingAppointments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ViewingAppointments",
                table: "ViewingAppointments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rooms",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_PostId",
                table: "Rooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Posts",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostImages",
                table: "PostImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostAmenities",
                table: "PostAmenities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Amenities",
                table: "Amenities");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PostAmenities");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Amenities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Amenities");

            migrationBuilder.RenameTable(
                name: "ViewingAppointments",
                newName: "LichHenXemPhong");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "TaiKhoan");

            migrationBuilder.RenameTable(
                name: "Rooms",
                newName: "PhongTro");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "__UnusedRoles");

            migrationBuilder.RenameTable(
                name: "Posts",
                newName: "TinDang");

            migrationBuilder.RenameTable(
                name: "PostImages",
                newName: "HinhAnhPhong");

            migrationBuilder.RenameTable(
                name: "PostAmenities",
                newName: "PhongTro_TienNghi");

            migrationBuilder.RenameTable(
                name: "Favorites",
                newName: "YeuThich");

            migrationBuilder.RenameTable(
                name: "Amenities",
                newName: "TienNghi");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "LichHenXemPhong",
                newName: "NgayCapNhat");

            migrationBuilder.RenameColumn(
                name: "TenantNote",
                table: "LichHenXemPhong",
                newName: "NoiDung");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "LichHenXemPhong",
                newName: "NguoiDungId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "LichHenXemPhong",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "ScheduledAt",
                table: "LichHenXemPhong",
                newName: "ThoiGianHen");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "LichHenXemPhong",
                newName: "TinDangId");

            migrationBuilder.RenameColumn(
                name: "LandlordResponse",
                table: "LichHenXemPhong",
                newName: "LyDoTuChoi");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LichHenXemPhong",
                newName: "NgayTao");

            migrationBuilder.RenameIndex(
                name: "IX_ViewingAppointments_TenantId",
                table: "LichHenXemPhong",
                newName: "IX_LichHenXemPhong_NguoiDungId");

            migrationBuilder.RenameIndex(
                name: "IX_ViewingAppointments_Status",
                table: "LichHenXemPhong",
                newName: "IX_LichHenXemPhong_TrangThai");

            migrationBuilder.RenameIndex(
                name: "IX_ViewingAppointments_ScheduledAt",
                table: "LichHenXemPhong",
                newName: "IX_LichHenXemPhong_ThoiGianHen");

            migrationBuilder.RenameIndex(
                name: "IX_ViewingAppointments_PostId",
                table: "LichHenXemPhong",
                newName: "IX_LichHenXemPhong_TinDangId");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "TaiKhoan",
                newName: "NgayCapNhat");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "TaiKhoan",
                newName: "VaiTro");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "TaiKhoan",
                newName: "SoDienThoai");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "TaiKhoan",
                newName: "MatKhauHash");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "TaiKhoan",
                newName: "HoTen");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "TaiKhoan",
                newName: "NgayTao");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "TaiKhoan",
                newName: "AnhDaiDien");

            migrationBuilder.RenameColumn(
                name: "Ward",
                table: "PhongTro",
                newName: "PhuongXa");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PhongTro",
                newName: "NgayCapNhat");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "PhongTro",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "PhongTro",
                newName: "TinhThanh");

            migrationBuilder.RenameColumn(
                name: "MaxOccupants",
                table: "PhongTro",
                newName: "SoNguoiToiDa");

            migrationBuilder.RenameColumn(
                name: "District",
                table: "PhongTro",
                newName: "QuanHuyen");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PhongTro",
                newName: "NgayTao");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "PhongTro",
                newName: "DienTich");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "PhongTro",
                newName: "DiaChi");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "PhongTro",
                newName: "SoNguoiHienTai");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_Status",
                table: "PhongTro",
                newName: "IX_PhongTro_TrangThai");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_Province",
                table: "PhongTro",
                newName: "IX_PhongTro_TinhThanh");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_District",
                table: "PhongTro",
                newName: "IX_PhongTro_QuanHuyen");

            migrationBuilder.RenameIndex(
                name: "IX_Roles_Name",
                table: "__UnusedRoles",
                newName: "IX___UnusedRoles_Name");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "TinDang",
                newName: "NgayCapNhat");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "TinDang",
                newName: "TieuDe");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "TinDang",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "TinDang",
                newName: "LyDoTuChoi");

            migrationBuilder.RenameColumn(
                name: "LandlordId",
                table: "TinDang",
                newName: "ChuTroId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "TinDang",
                newName: "NgayTao");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "TinDang",
                newName: "GiaHienThi");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_Status",
                table: "TinDang",
                newName: "IX_TinDang_TrangThai");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_LandlordId",
                table: "TinDang",
                newName: "IX_TinDang_ChuTroId");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "HinhAnhPhong",
                newName: "DuongDan");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "HinhAnhPhong",
                newName: "ThuTu");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "HinhAnhPhong",
                newName: "NgayTao");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "HinhAnhPhong",
                newName: "PhongTroId");

            migrationBuilder.RenameIndex(
                name: "IX_PostImages_PostId",
                table: "HinhAnhPhong",
                newName: "IX_HinhAnhPhong_PhongTroId");

            migrationBuilder.RenameIndex(
                name: "IX_PostImages_DisplayOrder",
                table: "HinhAnhPhong",
                newName: "IX_HinhAnhPhong_ThuTu");

            migrationBuilder.RenameColumn(
                name: "AmenityId",
                table: "PhongTro_TienNghi",
                newName: "TienNghiId");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "PhongTro_TienNghi",
                newName: "PhongTroId");

            migrationBuilder.RenameIndex(
                name: "IX_PostAmenities_AmenityId",
                table: "PhongTro_TienNghi",
                newName: "IX_PhongTro_TienNghi_TienNghiId");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "YeuThich",
                newName: "TinDangId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "YeuThich",
                newName: "NgayLuu");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "YeuThich",
                newName: "NguoiDungId");

            migrationBuilder.RenameIndex(
                name: "IX_Favorites_UserId_PostId",
                table: "YeuThich",
                newName: "IX_YeuThich_NguoiDungId_TinDangId");

            migrationBuilder.RenameIndex(
                name: "IX_Favorites_PostId",
                table: "YeuThich",
                newName: "IX_YeuThich_TinDangId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "TienNghi",
                newName: "TenTienNghi");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "TienNghi",
                newName: "MoTa");

            migrationBuilder.RenameIndex(
                name: "IX_Amenities_Name",
                table: "TienNghi",
                newName: "IX_TienNghi_TenTienNghi");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "LichHenXemPhong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "ChuTroId",
                table: "LichHenXemPhong",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "TaiKhoan",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "VaiTro",
                table: "TaiKhoan",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SoDienThoai",
                table: "TaiKhoan",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "MatKhauHash",
                table: "TaiKhoan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "TaiKhoan",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "TaiKhoan",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "TenDangNhap",
                table: "TaiKhoan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TrangThai",
                table: "TaiKhoan",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhuongXa",
                table: "PhongTro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "TinhThanh",
                table: "PhongTro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "QuanHuyen",
                table: "PhongTro",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "PhongTro",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "DiaChi",
                table: "PhongTro",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<int>(
                name: "ChuTroId",
                table: "PhongTro",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DanhMucId",
                table: "PhongTro",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaThue",
                table: "PhongTro",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KinhDo",
                table: "PhongTro",
                type: "decimal(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "PhongTro",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiDichVu",
                table: "PhongTro",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoPhongNgu",
                table: "PhongTro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoPhongTam",
                table: "PhongTro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tang",
                table: "PhongTro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenPhong",
                table: "PhongTro",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TienDien",
                table: "PhongTro",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TienNuoc",
                table: "PhongTro",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ViDo",
                table: "PhongTro",
                type: "decimal(10,7)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TieuDe",
                table: "TinDang",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "TinDang",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "LuotXem",
                table: "TinDang",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDang",
                table: "TinDang",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyet",
                table: "TinDang",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayHetHan",
                table: "TinDang",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiDung",
                table: "TinDang",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PhongTroId",
                table: "TinDang",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "DuongDan",
                table: "HinhAnhPhong",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayTao",
                table: "HinhAnhPhong",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "LaAnhDaiDien",
                table: "HinhAnhPhong",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgayLuu",
                table: "YeuThich",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "TienNghi",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "TienNghi",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrangThai",
                table: "TienNghi",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LichHenXemPhong",
                table: "LichHenXemPhong",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaiKhoan",
                table: "TaiKhoan",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhongTro",
                table: "PhongTro",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK___UnusedRoles",
                table: "__UnusedRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TinDang",
                table: "TinDang",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HinhAnhPhong",
                table: "HinhAnhPhong",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhongTro_TienNghi",
                table: "PhongTro_TienNghi",
                columns: new[] { "PhongTroId", "TienNghiId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_YeuThich",
                table: "YeuThich",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TienNghi",
                table: "TienNghi",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BaiViet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: false),
                    TomTat = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TacGiaId = table.Column<int>(type: "int", nullable: false),
                    LuotXem = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NgayDang = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiViet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaiViet_TaiKhoan_TacGiaId",
                        column: x => x.TacGiaId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaoCaoTinDang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TinDangId = table.Column<int>(type: "int", nullable: false),
                    NguoiBaoCaoId = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NgayBaoCao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiXuLyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoTinDang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaoCaoTinDang_TaiKhoan_NguoiBaoCaoId",
                        column: x => x.NguoiBaoCaoId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaoCaoTinDang_TaiKhoan_NguoiXuLyId",
                        column: x => x.NguoiXuLyId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaoCaoTinDang_TinDang_TinDangId",
                        column: x => x.TinDangId,
                        principalTable: "TinDang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChuTro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    GioiThieu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SoCanCuoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DaXacThuc = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NgayXacThuc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuTro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChuTro_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DanhMucPhong",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMucPhong", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NgheNghiep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChiHienTai = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    GioiThieu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NguoiDung_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LoaiThongBao = table.Column<int>(type: "int", nullable: true),
                    LienKet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BinhLuan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaiVietId = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinhLuan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinhLuan_BaiViet_BaiVietId",
                        column: x => x.BaiVietId,
                        principalTable: "BaiViet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BinhLuan_TaiKhoan_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichHenXemPhong_ChuTroId",
                table: "LichHenXemPhong",
                column: "ChuTroId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_Email",
                table: "TaiKhoan",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhongTro_ChuTroId",
                table: "PhongTro",
                column: "ChuTroId");

            migrationBuilder.CreateIndex(
                name: "IX_PhongTro_DanhMucId",
                table: "PhongTro",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_TinDang_PhongTroId",
                table: "TinDang",
                column: "PhongTroId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiViet_Slug",
                table: "BaiViet",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaiViet_TacGiaId",
                table: "BaiViet",
                column: "TacGiaId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTinDang_NguoiBaoCaoId",
                table: "BaoCaoTinDang",
                column: "NguoiBaoCaoId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTinDang_NguoiXuLyId",
                table: "BaoCaoTinDang",
                column: "NguoiXuLyId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTinDang_TinDangId",
                table: "BaoCaoTinDang",
                column: "TinDangId");

            migrationBuilder.CreateIndex(
                name: "IX_BinhLuan_BaiVietId",
                table: "BinhLuan",
                column: "BaiVietId");

            migrationBuilder.CreateIndex(
                name: "IX_BinhLuan_TaiKhoanId",
                table: "BinhLuan",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuTro_SoCanCuoc",
                table: "ChuTro",
                column: "SoCanCuoc",
                unique: true,
                filter: "[SoCanCuoc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChuTro_TaiKhoanId",
                table: "ChuTro",
                column: "TaiKhoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_TaiKhoanId",
                table: "NguoiDung",
                column: "TaiKhoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_TaiKhoanId",
                table: "ThongBao",
                column: "TaiKhoanId");

            migrationBuilder.AddForeignKey(
                name: "FK_HinhAnhPhong_PhongTro_PhongTroId",
                table: "HinhAnhPhong",
                column: "PhongTroId",
                principalTable: "PhongTro",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHenXemPhong_ChuTro_ChuTroId",
                table: "LichHenXemPhong",
                column: "ChuTroId",
                principalTable: "ChuTro",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHenXemPhong_NguoiDung_NguoiDungId",
                table: "LichHenXemPhong",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHenXemPhong_TinDang_TinDangId",
                table: "LichHenXemPhong",
                column: "TinDangId",
                principalTable: "TinDang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhongTro_ChuTro_ChuTroId",
                table: "PhongTro",
                column: "ChuTroId",
                principalTable: "ChuTro",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhongTro_DanhMucPhong_DanhMucId",
                table: "PhongTro",
                column: "DanhMucId",
                principalTable: "DanhMucPhong",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhongTro_TienNghi_PhongTro_PhongTroId",
                table: "PhongTro_TienNghi",
                column: "PhongTroId",
                principalTable: "PhongTro",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhongTro_TienNghi_TienNghi_TienNghiId",
                table: "PhongTro_TienNghi",
                column: "TienNghiId",
                principalTable: "TienNghi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TinDang_ChuTro_ChuTroId",
                table: "TinDang",
                column: "ChuTroId",
                principalTable: "ChuTro",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TinDang_PhongTro_PhongTroId",
                table: "TinDang",
                column: "PhongTroId",
                principalTable: "PhongTro",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuThich_NguoiDung_NguoiDungId",
                table: "YeuThich",
                column: "NguoiDungId",
                principalTable: "NguoiDung",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_YeuThich_TinDang_TinDangId",
                table: "YeuThich",
                column: "TinDangId",
                principalTable: "TinDang",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HinhAnhPhong_PhongTro_PhongTroId",
                table: "HinhAnhPhong");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHenXemPhong_ChuTro_ChuTroId",
                table: "LichHenXemPhong");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHenXemPhong_NguoiDung_NguoiDungId",
                table: "LichHenXemPhong");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHenXemPhong_TinDang_TinDangId",
                table: "LichHenXemPhong");

            migrationBuilder.DropForeignKey(
                name: "FK_PhongTro_ChuTro_ChuTroId",
                table: "PhongTro");

            migrationBuilder.DropForeignKey(
                name: "FK_PhongTro_DanhMucPhong_DanhMucId",
                table: "PhongTro");

            migrationBuilder.DropForeignKey(
                name: "FK_PhongTro_TienNghi_PhongTro_PhongTroId",
                table: "PhongTro_TienNghi");

            migrationBuilder.DropForeignKey(
                name: "FK_PhongTro_TienNghi_TienNghi_TienNghiId",
                table: "PhongTro_TienNghi");

            migrationBuilder.DropForeignKey(
                name: "FK_TinDang_ChuTro_ChuTroId",
                table: "TinDang");

            migrationBuilder.DropForeignKey(
                name: "FK_TinDang_PhongTro_PhongTroId",
                table: "TinDang");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuThich_NguoiDung_NguoiDungId",
                table: "YeuThich");

            migrationBuilder.DropForeignKey(
                name: "FK_YeuThich_TinDang_TinDangId",
                table: "YeuThich");

            migrationBuilder.DropTable(
                name: "BaoCaoTinDang");

            migrationBuilder.DropTable(
                name: "BinhLuan");

            migrationBuilder.DropTable(
                name: "ChuTro");

            migrationBuilder.DropTable(
                name: "DanhMucPhong");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "BaiViet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_YeuThich",
                table: "YeuThich");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TinDang",
                table: "TinDang");

            migrationBuilder.DropIndex(
                name: "IX_TinDang_PhongTroId",
                table: "TinDang");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TienNghi",
                table: "TienNghi");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaiKhoan",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_Email",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhongTro_TienNghi",
                table: "PhongTro_TienNghi");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhongTro",
                table: "PhongTro");

            migrationBuilder.DropIndex(
                name: "IX_PhongTro_ChuTroId",
                table: "PhongTro");

            migrationBuilder.DropIndex(
                name: "IX_PhongTro_DanhMucId",
                table: "PhongTro");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LichHenXemPhong",
                table: "LichHenXemPhong");

            migrationBuilder.DropIndex(
                name: "IX_LichHenXemPhong_ChuTroId",
                table: "LichHenXemPhong");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HinhAnhPhong",
                table: "HinhAnhPhong");

            migrationBuilder.DropPrimaryKey(
                name: "PK___UnusedRoles",
                table: "__UnusedRoles");

            migrationBuilder.DropColumn(
                name: "LuotXem",
                table: "TinDang");

            migrationBuilder.DropColumn(
                name: "NgayDang",
                table: "TinDang");

            migrationBuilder.DropColumn(
                name: "NgayDuyet",
                table: "TinDang");

            migrationBuilder.DropColumn(
                name: "NgayHetHan",
                table: "TinDang");

            migrationBuilder.DropColumn(
                name: "NoiDung",
                table: "TinDang");

            migrationBuilder.DropColumn(
                name: "PhongTroId",
                table: "TinDang");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "TienNghi");

            migrationBuilder.DropColumn(
                name: "TenDangNhap",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "TaiKhoan");

            migrationBuilder.DropColumn(
                name: "ChuTroId",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "DanhMucId",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "GiaThue",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "KinhDo",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "PhiDichVu",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "SoPhongNgu",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "SoPhongTam",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "Tang",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "TenPhong",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "TienDien",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "TienNuoc",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "ViDo",
                table: "PhongTro");

            migrationBuilder.DropColumn(
                name: "ChuTroId",
                table: "LichHenXemPhong");

            migrationBuilder.DropColumn(
                name: "LaAnhDaiDien",
                table: "HinhAnhPhong");

            migrationBuilder.RenameTable(
                name: "YeuThich",
                newName: "Favorites");

            migrationBuilder.RenameTable(
                name: "TinDang",
                newName: "Posts");

            migrationBuilder.RenameTable(
                name: "TienNghi",
                newName: "Amenities");

            migrationBuilder.RenameTable(
                name: "TaiKhoan",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "PhongTro_TienNghi",
                newName: "PostAmenities");

            migrationBuilder.RenameTable(
                name: "PhongTro",
                newName: "Rooms");

            migrationBuilder.RenameTable(
                name: "LichHenXemPhong",
                newName: "ViewingAppointments");

            migrationBuilder.RenameTable(
                name: "HinhAnhPhong",
                newName: "PostImages");

            migrationBuilder.RenameTable(
                name: "__UnusedRoles",
                newName: "Roles");

            migrationBuilder.RenameColumn(
                name: "TinDangId",
                table: "Favorites",
                newName: "PostId");

            migrationBuilder.RenameColumn(
                name: "NgayLuu",
                table: "Favorites",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "NguoiDungId",
                table: "Favorites",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_YeuThich_TinDangId",
                table: "Favorites",
                newName: "IX_Favorites_PostId");

            migrationBuilder.RenameIndex(
                name: "IX_YeuThich_NguoiDungId_TinDangId",
                table: "Favorites",
                newName: "IX_Favorites_UserId_PostId");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "Posts",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "TieuDe",
                table: "Posts",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "NgayTao",
                table: "Posts",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "NgayCapNhat",
                table: "Posts",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "LyDoTuChoi",
                table: "Posts",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "ChuTroId",
                table: "Posts",
                newName: "LandlordId");

            migrationBuilder.RenameColumn(
                name: "GiaHienThi",
                table: "Posts",
                newName: "Price");

            migrationBuilder.RenameIndex(
                name: "IX_TinDang_TrangThai",
                table: "Posts",
                newName: "IX_Posts_Status");

            migrationBuilder.RenameIndex(
                name: "IX_TinDang_ChuTroId",
                table: "Posts",
                newName: "IX_Posts_LandlordId");

            migrationBuilder.RenameColumn(
                name: "TenTienNghi",
                table: "Amenities",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "Amenities",
                newName: "Description");

            migrationBuilder.RenameIndex(
                name: "IX_TienNghi_TenTienNghi",
                table: "Amenities",
                newName: "IX_Amenities_Name");

            migrationBuilder.RenameColumn(
                name: "VaiTro",
                table: "Users",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "SoDienThoai",
                table: "Users",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "NgayTao",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "NgayCapNhat",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "MatKhauHash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "HoTen",
                table: "Users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "AnhDaiDien",
                table: "Users",
                newName: "AvatarUrl");

            migrationBuilder.RenameColumn(
                name: "TienNghiId",
                table: "PostAmenities",
                newName: "AmenityId");

            migrationBuilder.RenameColumn(
                name: "PhongTroId",
                table: "PostAmenities",
                newName: "PostId");

            migrationBuilder.RenameIndex(
                name: "IX_PhongTro_TienNghi_TienNghiId",
                table: "PostAmenities",
                newName: "IX_PostAmenities_AmenityId");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "Rooms",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "TinhThanh",
                table: "Rooms",
                newName: "Province");

            migrationBuilder.RenameColumn(
                name: "SoNguoiToiDa",
                table: "Rooms",
                newName: "MaxOccupants");

            migrationBuilder.RenameColumn(
                name: "QuanHuyen",
                table: "Rooms",
                newName: "District");

            migrationBuilder.RenameColumn(
                name: "PhuongXa",
                table: "Rooms",
                newName: "Ward");

            migrationBuilder.RenameColumn(
                name: "NgayTao",
                table: "Rooms",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "NgayCapNhat",
                table: "Rooms",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "DienTich",
                table: "Rooms",
                newName: "Area");

            migrationBuilder.RenameColumn(
                name: "DiaChi",
                table: "Rooms",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "SoNguoiHienTai",
                table: "Rooms",
                newName: "PostId");

            migrationBuilder.RenameIndex(
                name: "IX_PhongTro_TrangThai",
                table: "Rooms",
                newName: "IX_Rooms_Status");

            migrationBuilder.RenameIndex(
                name: "IX_PhongTro_TinhThanh",
                table: "Rooms",
                newName: "IX_Rooms_Province");

            migrationBuilder.RenameIndex(
                name: "IX_PhongTro_QuanHuyen",
                table: "Rooms",
                newName: "IX_Rooms_District");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "ViewingAppointments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "TinDangId",
                table: "ViewingAppointments",
                newName: "PostId");

            migrationBuilder.RenameColumn(
                name: "ThoiGianHen",
                table: "ViewingAppointments",
                newName: "ScheduledAt");

            migrationBuilder.RenameColumn(
                name: "NoiDung",
                table: "ViewingAppointments",
                newName: "TenantNote");

            migrationBuilder.RenameColumn(
                name: "NguoiDungId",
                table: "ViewingAppointments",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "NgayTao",
                table: "ViewingAppointments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "NgayCapNhat",
                table: "ViewingAppointments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "LyDoTuChoi",
                table: "ViewingAppointments",
                newName: "LandlordResponse");

            migrationBuilder.RenameIndex(
                name: "IX_LichHenXemPhong_TrangThai",
                table: "ViewingAppointments",
                newName: "IX_ViewingAppointments_Status");

            migrationBuilder.RenameIndex(
                name: "IX_LichHenXemPhong_TinDangId",
                table: "ViewingAppointments",
                newName: "IX_ViewingAppointments_PostId");

            migrationBuilder.RenameIndex(
                name: "IX_LichHenXemPhong_ThoiGianHen",
                table: "ViewingAppointments",
                newName: "IX_ViewingAppointments_ScheduledAt");

            migrationBuilder.RenameIndex(
                name: "IX_LichHenXemPhong_NguoiDungId",
                table: "ViewingAppointments",
                newName: "IX_ViewingAppointments_TenantId");

            migrationBuilder.RenameColumn(
                name: "ThuTu",
                table: "PostImages",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "NgayTao",
                table: "PostImages",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "DuongDan",
                table: "PostImages",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "PhongTroId",
                table: "PostImages",
                newName: "PostId");

            migrationBuilder.RenameIndex(
                name: "IX_HinhAnhPhong_ThuTu",
                table: "PostImages",
                newName: "IX_PostImages_DisplayOrder");

            migrationBuilder.RenameIndex(
                name: "IX_HinhAnhPhong_PhongTroId",
                table: "PostImages",
                newName: "IX_PostImages_PostId");

            migrationBuilder.RenameIndex(
                name: "IX___UnusedRoles_Name",
                table: "Roles",
                newName: "IX_Roles_Name");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Favorites",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Posts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Amenities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Amenities",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Amenities",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Amenities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PostAmenities",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Province",
                table: "Rooms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "Rooms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ward",
                table: "Rooms",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Rooms",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Rooms",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ViewingAppointments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PostImages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "PostImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Posts",
                table: "Posts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Amenities",
                table: "Amenities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostAmenities",
                table: "PostAmenities",
                columns: new[] { "PostId", "AmenityId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rooms",
                table: "Rooms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ViewingAppointments",
                table: "ViewingAppointments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostImages",
                table: "PostImages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Người tìm trọ - Có thể tìm kiếm, xem phòng và đặt lịch xem phòng", "Tenant" },
                    { 2, "Chủ nhà trọ - Có thể đăng tin cho thuê phòng và quản lý tin đăng", "Landlord" },
                    { 3, "Quản trị viên - Quản lý toàn bộ hệ thống, duyệt tin đăng", "Admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PostId",
                table: "Rooms",
                column: "PostId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Posts_PostId",
                table: "Favorites",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Users_UserId",
                table: "Favorites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostAmenities_Amenities_AmenityId",
                table: "PostAmenities",
                column: "AmenityId",
                principalTable: "Amenities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostAmenities_Posts_PostId",
                table: "PostAmenities",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostImages_Posts_PostId",
                table: "PostImages",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Users_LandlordId",
                table: "Posts",
                column: "LandlordId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Posts_PostId",
                table: "Rooms",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ViewingAppointments_Posts_PostId",
                table: "ViewingAppointments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ViewingAppointments_Users_TenantId",
                table: "ViewingAppointments",
                column: "TenantId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
