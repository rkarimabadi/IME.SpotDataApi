namespace IME.SpotDataApi.Models.DTO
{
    public class OfferDetailDto
    {
        public int Id { get; set; }
        public string OfferSymbol { get; set; }
        public string Description { get; set; }
        public string OfferDate { get; set; }
        public string DeliveryDate { get; set; }
        public decimal InitPrice { get; set; }
        public decimal OfferVol { get; set; }
        public string TradeStatus { get; set; }
        public string? CommodityName { get; set; }
        public string? SupplierName { get; set; }
        public string? BrokerName { get; set; }
        public string? ManufacturerName { get; set; }
        public string? CurrencyUnitName { get; set; }
        public string? MeasurementUnitName { get; set; }
        public string? ContractTypeName { get; set; }
    }
}

