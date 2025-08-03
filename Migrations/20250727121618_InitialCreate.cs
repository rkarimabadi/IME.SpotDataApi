using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IME.SpotDataApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brokers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NationalId = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brokers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuyMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuyMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commodities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commodities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryPlaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPlaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MainGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Manufacturers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PriorTitle = table.Column<string>(type: "TEXT", nullable: true),
                    ShortAbstract = table.Column<string>(type: "TEXT", nullable: true),
                    SubTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    FirstPicture = table.Column<string>(type: "TEXT", nullable: true),
                    MainTitle = table.Column<string>(type: "TEXT", nullable: true),
                    NewsDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    URL = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferModes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferModes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackagingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagingTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpotNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MainTitle = table.Column<string>(type: "TEXT", nullable: true),
                    NewsDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    URL = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    NationalCode = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    SettlementTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryDate = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveryPlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    CommodityId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxIncreaseDemand = table.Column<int>(type: "INTEGER", nullable: false),
                    LoadFactor = table.Column<decimal>(type: "TEXT", nullable: false),
                    LotCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LotSize = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxBuyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxSellPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinBuyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinSellPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    PackagingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    TenderApplicantId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrokerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenderDate = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SecurityTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingHalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PersianName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingHalls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrokerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyMethodId = table.Column<int>(type: "INTEGER", nullable: false),
                    CommodityId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContractTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryDate = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveryPlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    InitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    InitVolume = table.Column<decimal>(type: "TEXT", nullable: false),
                    LotSize = table.Column<decimal>(type: "TEXT", nullable: false),
                    ManufacturerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxInitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxIncOfferVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxOrderVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxOfferPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MeasureUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeasurementUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinAllocationVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinOfferVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinInitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinOrderVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinOfferPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    OfferDate = table.Column<string>(type: "TEXT", nullable: false),
                    OfferModeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferRing = table.Column<string>(type: "TEXT", nullable: false),
                    OfferSymbol = table.Column<string>(type: "TEXT", nullable: false),
                    OfferTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    PackagingTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissibleError = table.Column<decimal>(type: "TEXT", nullable: false),
                    PriceDiscoveryMinOrderVol = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrepaymentPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    SecurityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityTypeNote = table.Column<string>(type: "TEXT", nullable: false),
                    SettlementTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    TickSize = table.Column<decimal>(type: "TEXT", nullable: false),
                    TradingHallId = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeStatus = table.Column<string>(type: "TEXT", nullable: false),
                    WeightFactor = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_ContractTypes_ContractTypeId",
                        column: x => x.ContractTypeId,
                        principalTable: "ContractTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_CurrencyUnits_CurrencyUnitId",
                        column: x => x.CurrencyUnitId,
                        principalTable: "CurrencyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_MeasurementUnits_MeasurementUnitId",
                        column: x => x.MeasurementUnitId,
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BuyMethod = table.Column<string>(type: "TEXT", nullable: false),
                    CommodityId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferSymbol = table.Column<string>(type: "TEXT", nullable: false),
                    ContractTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfferType = table.Column<string>(type: "TEXT", nullable: false),
                    OfferMode = table.Column<string>(type: "TEXT", nullable: false),
                    DemandVolume = table.Column<decimal>(type: "TEXT", nullable: false),
                    OfferVolume = table.Column<decimal>(type: "TEXT", nullable: false),
                    FinalWeightedAveragePrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    DueDate = table.Column<string>(type: "TEXT", nullable: false),
                    ManufacturerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeasurementUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MinimumPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    OfferBasePrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    SellerBrokerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeDate = table.Column<string>(type: "TEXT", nullable: false),
                    TradeValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    TradeVolume = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeReports_Brokers_SellerBrokerId",
                        column: x => x.SellerBrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_Commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "Commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_ContractTypes_ContractTypeId",
                        column: x => x.ContractTypeId,
                        principalTable: "ContractTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_CurrencyUnits_CurrencyUnitId",
                        column: x => x.CurrencyUnitId,
                        principalTable: "CurrencyUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_MeasurementUnits_MeasurementUnitId",
                        column: x => x.MeasurementUnitId,
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeReports_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuyMethods");

            migrationBuilder.DropTable(
                name: "DeliveryPlaces");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "MainGroups");

            migrationBuilder.DropTable(
                name: "NewsNotifications");

            migrationBuilder.DropTable(
                name: "OfferModes");

            migrationBuilder.DropTable(
                name: "OfferTypes");

            migrationBuilder.DropTable(
                name: "PackagingTypes");

            migrationBuilder.DropTable(
                name: "SecurityTypes");

            migrationBuilder.DropTable(
                name: "SettlementTypes");

            migrationBuilder.DropTable(
                name: "SpotNotifications");

            migrationBuilder.DropTable(
                name: "SubGroups");

            migrationBuilder.DropTable(
                name: "Tenders");

            migrationBuilder.DropTable(
                name: "TradeReports");

            migrationBuilder.DropTable(
                name: "TradingHalls");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "Brokers");

            migrationBuilder.DropTable(
                name: "Commodities");

            migrationBuilder.DropTable(
                name: "ContractTypes");

            migrationBuilder.DropTable(
                name: "CurrencyUnits");

            migrationBuilder.DropTable(
                name: "Manufacturers");

            migrationBuilder.DropTable(
                name: "MeasurementUnits");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
