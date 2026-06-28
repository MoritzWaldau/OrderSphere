using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderSphere.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponTiersAndCategoryScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "scoped_category_ids",
                table: "coupons",
                type: "uuid[]",
                nullable: false,
                defaultValue: new List<Guid>());

            migrationBuilder.AddColumn<string>(
                name: "tiers",
                table: "coupons",
                type: "jsonb",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "coupons",
                keyColumn: "Id",
                keyValue: new Guid("0192a000-0000-7000-8000-000000000001"),
                column: "scoped_category_ids",
                value: new List<Guid>());

            migrationBuilder.UpdateData(
                table: "coupons",
                keyColumn: "Id",
                keyValue: new Guid("0192a000-0000-7000-8000-000000000002"),
                column: "scoped_category_ids",
                value: new List<Guid>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "scoped_category_ids",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "tiers",
                table: "coupons");
        }
    }
}
