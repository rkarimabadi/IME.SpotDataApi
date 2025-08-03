using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IME.SpotDataApi.Migrations
{
    /// <inheritdoc />
    public partial class fk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Brokers_BrokerId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Commodities_CommodityId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_ContractTypes_ContractTypeId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_CurrencyUnits_CurrencyUnitId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Manufacturers_ManufacturerId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_MeasurementUnits_MeasurementUnitId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Suppliers_SupplierId",
                table: "Offers");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_Brokers_SellerBrokerId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_Commodities_CommodityId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_ContractTypes_ContractTypeId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_CurrencyUnits_CurrencyUnitId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_Manufacturers_ManufacturerId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_MeasurementUnits_MeasurementUnitId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_Offers_OfferId",
                table: "TradeReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TradeReports_Suppliers_SupplierId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_CommodityId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_ContractTypeId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_CurrencyUnitId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_ManufacturerId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_MeasurementUnitId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_OfferId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_SellerBrokerId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_TradeReports_SupplierId",
                table: "TradeReports");

            migrationBuilder.DropIndex(
                name: "IX_Offers_BrokerId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_CommodityId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_ContractTypeId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_CurrencyUnitId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_ManufacturerId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_MeasurementUnitId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_SupplierId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CurrencyUnitId",
                table: "TradeReports");

            migrationBuilder.DropColumn(
                name: "CurrencyUnitId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "MeasurementUnitId",
                table: "Offers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "TradeReports",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "TradeReports",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyUnitId",
                table: "TradeReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyUnitId",
                table: "Offers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementUnitId",
                table: "Offers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_CommodityId",
                table: "TradeReports",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_ContractTypeId",
                table: "TradeReports",
                column: "ContractTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_CurrencyUnitId",
                table: "TradeReports",
                column: "CurrencyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_ManufacturerId",
                table: "TradeReports",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_MeasurementUnitId",
                table: "TradeReports",
                column: "MeasurementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_OfferId",
                table: "TradeReports",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_SellerBrokerId",
                table: "TradeReports",
                column: "SellerBrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeReports_SupplierId",
                table: "TradeReports",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_BrokerId",
                table: "Offers",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CommodityId",
                table: "Offers",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ContractTypeId",
                table: "Offers",
                column: "ContractTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CurrencyUnitId",
                table: "Offers",
                column: "CurrencyUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ManufacturerId",
                table: "Offers",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_MeasurementUnitId",
                table: "Offers",
                column: "MeasurementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_SupplierId",
                table: "Offers",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Brokers_BrokerId",
                table: "Offers",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Commodities_CommodityId",
                table: "Offers",
                column: "CommodityId",
                principalTable: "Commodities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_ContractTypes_ContractTypeId",
                table: "Offers",
                column: "ContractTypeId",
                principalTable: "ContractTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_CurrencyUnits_CurrencyUnitId",
                table: "Offers",
                column: "CurrencyUnitId",
                principalTable: "CurrencyUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Manufacturers_ManufacturerId",
                table: "Offers",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_MeasurementUnits_MeasurementUnitId",
                table: "Offers",
                column: "MeasurementUnitId",
                principalTable: "MeasurementUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Suppliers_SupplierId",
                table: "Offers",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_Brokers_SellerBrokerId",
                table: "TradeReports",
                column: "SellerBrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_Commodities_CommodityId",
                table: "TradeReports",
                column: "CommodityId",
                principalTable: "Commodities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_ContractTypes_ContractTypeId",
                table: "TradeReports",
                column: "ContractTypeId",
                principalTable: "ContractTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_CurrencyUnits_CurrencyUnitId",
                table: "TradeReports",
                column: "CurrencyUnitId",
                principalTable: "CurrencyUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_Manufacturers_ManufacturerId",
                table: "TradeReports",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_MeasurementUnits_MeasurementUnitId",
                table: "TradeReports",
                column: "MeasurementUnitId",
                principalTable: "MeasurementUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_Offers_OfferId",
                table: "TradeReports",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TradeReports_Suppliers_SupplierId",
                table: "TradeReports",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
