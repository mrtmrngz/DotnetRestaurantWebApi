using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_RestaurantTableId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ProductImageUrl",
                table: "order_items");

            migrationBuilder.AlterColumn<string>(
                name: "AddressSnapshot",
                table: "orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckoutSessionExpiresAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionId",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionUrl",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "order_items",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "order_items",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "ProductSnapshot",
                table: "order_items",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CheckoutSessionId",
                table: "orders",
                column: "CheckoutSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_RestaurantTableId_IdempotencyKey",
                table: "orders",
                columns: new[] { "RestaurantTableId", "IdempotencyKey" },
                unique: true,
                filter: "\"RestaurantTableId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId_IdempotencyKey",
                table: "orders",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_CheckoutSessionId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_RestaurantTableId_IdempotencyKey",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_UserId_IdempotencyKey",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionExpiresAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionUrl",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ProductSnapshot",
                table: "order_items");

            migrationBuilder.AlterColumn<string>(
                name: "AddressSnapshot",
                table: "orders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "order_items",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "order_items",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "ProductImageUrl",
                table: "order_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_orders_RestaurantTableId",
                table: "orders",
                column: "RestaurantTableId");
        }
    }
}
