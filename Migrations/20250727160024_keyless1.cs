using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IME.SpotDataApi.Migrations
{
    /// <inheritdoc />
    public partial class keyless1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SpotNotifications",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "NewsNotifications",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TradingHalls",
                table: "TradingHalls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TradeReports",
                table: "TradeReports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tenders",
                table: "Tenders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubGroups",
                table: "SubGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpotNotifications",
                table: "SpotNotifications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SettlementTypes",
                table: "SettlementTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SecurityTypes",
                table: "SecurityTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PackagingTypes",
                table: "PackagingTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OfferTypes",
                table: "OfferTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Offers",
                table: "Offers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OfferModes",
                table: "OfferModes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsNotifications",
                table: "NewsNotifications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeasurementUnits",
                table: "MeasurementUnits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Manufacturers",
                table: "Manufacturers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MainGroups",
                table: "MainGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Groups",
                table: "Groups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeliveryPlaces",
                table: "DeliveryPlaces",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CurrencyUnits",
                table: "CurrencyUnits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContractTypes",
                table: "ContractTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Commodities",
                table: "Commodities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BuyMethods",
                table: "BuyMethods",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Brokers",
                table: "Brokers",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TradingHalls",
                table: "TradingHalls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TradeReports",
                table: "TradeReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tenders",
                table: "Tenders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubGroups",
                table: "SubGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SpotNotifications",
                table: "SpotNotifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SettlementTypes",
                table: "SettlementTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SecurityTypes",
                table: "SecurityTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PackagingTypes",
                table: "PackagingTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OfferTypes",
                table: "OfferTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Offers",
                table: "Offers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OfferModes",
                table: "OfferModes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsNotifications",
                table: "NewsNotifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MeasurementUnits",
                table: "MeasurementUnits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Manufacturers",
                table: "Manufacturers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MainGroups",
                table: "MainGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Groups",
                table: "Groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeliveryPlaces",
                table: "DeliveryPlaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CurrencyUnits",
                table: "CurrencyUnits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContractTypes",
                table: "ContractTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Commodities",
                table: "Commodities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BuyMethods",
                table: "BuyMethods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Brokers",
                table: "Brokers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SpotNotifications",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "NewsNotifications",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }
    }
}
