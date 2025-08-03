namespace IME.SpotDataApi.Models.Core
{
    public static class Constants
    {
        public const string DefaultLegacyNamespace = "http://tempuri.org/";

        public static int MaxCountOfContractDetails = 1000;

        public static class MediaTypeNames
        {
            public const string ApplicationXml = "application/xml";
            public const string TextXml = "text/xml";
            public const string ApplicationJson = "application/json";
            public const string TextJson = "text/json";
        }

        public static class Paging
        {
            public const int MinPageSize = 10;
            public const int MaxPageSize = 200;
            public const int DefaultPageSize = 50;
            public const int MinPageNumber = 1;
        }

        public static class CommonParameterNames
        {
            public const string PageNumber = "pageNumber";
            public const string PageSize = "pageSize";
            public const string PersianDate = "persianDate";
            public const string Id = "id";
            public const string SpotOrderStatus = "status";
            public const string ParentId = "parentId";
            public const string Language = "Language";
            public const string MarketType = "marketType";
            public const string Period = "period";
            public const string NationalId = "nationalId";
        }

        public static class CommonLinkRelValues
        {
            public const string Self = "self";
            public const string All = "all";
            public const string CurrentPage = "currentPage";
            public const string PreviousPage = "previousPage";
            public const string NextPage = "nextPage";
        }

        public static class CommonRoutingDefinitions
        {
            public const string ApiSegmentName = "api";
            public const string ApiVersionSegmentName = "apiVersion";
            public const string CurrentApiVersion = "v1";
        }

        public static class SchemeTypes
        {
            public const string PublicScheme = "publicScheme";
            public const string BrokerScheme = "brokerScheme";
            public const string SupplierScheme = "supplierScheme";
        }

        public static class Scopes
        {
            public const string PublicScope = "InformingApiScope1";
            public const string BrokerOrderScope = "BrokerServiceApiReadWriteScope";
        }

        public static class RoleNames
        {
            public const string Public = "subscriber";
            public const string Broker = "broker";
            public const string Supplier = "supplier";
            public const string Admin = "admin";
            public const string Report = "report";
            public const string SpotOrder = "spotorder";
        }

        public static class PolicyNames
        {
            public const string SpotOrder = "spotorder.owner";
            public const string SpotCustomerRequest = "spotCustomerRequest.owner";
            public const string PublicSchemePolicy = "PublicSchemePolicy";
            public const string BrokerSchemePolicy = "BrokerSchemePolicy";
        }

        public class LoggingEvents
        {
            public const int GeneralItems = 1000;
            public const int ListItems = 1001;
            public const int GetItem = 1002;
            public const int InsertItem = 1003;
            public const int UpdateItem = 1004;
            public const int DeleteItem = 1005;

            public const int GetItemNotFound = 4000;
            public const int UpdateItemNotFound = 4001;
        }

        public class CacheKeys
        {
            public const string SpotTrade = "spotTrades";
            public const string SpotSettlement = "spotSettlements";
            public const string SpotBrokerCustomer = "spotBrokerCustomers";
            public const string BrokerTradingReport = "brokerTradingReport";
            public const string DerivativeTrade = "derivativeTrade";
            public const string TradingReport = "tradingReport";
            public const string Derivative = "derivative";
            public const string Spot = "spot";
            public const string SpotOrder = "spotOrder";
            public const string AllCustomers = "allCustomers";
        }

        public class ApiCorsPolicies
        {
            public const string AllowedOriginsPolicy = "AllowedOrigins";
            public const string AllowAll = "AllowAll";
        }

        public class BaseInfoType
        {
            public const string OfferType = "offerType ";
            public const string SecurityType = "securityType";
            public const string BuyMethod = "buyMethod ";
            public const string SettlementType = "settlementType";
            public const string OfferMode = "offerMode";
            public const string ContractType = "contractType";
            public const string DeliveryPlace = "deliveryPlace";
            public const string PackagingType = "packagingType";
            public const string Ring = "ring";
            public const string Manufacturer = "manufacturer";
            public const string MeasurementUnit = "measurementUnit";
            public const string Currency = "currency";
            public const string MainGroup = "mainGroup";
            public const string MiddleGroup = "middleGroup";
            public const string SubGroup = "subGroup";
            public const string Commodity = "commodity";
            public const string Broker = "broker";
            public const string Supplier = "supplier";
            public const string TradingHall = "tradingHall";
        }

        public static class Translation
        {
            public const string DefaultLanguage = "fa";
            public const string CacheKey = "dictionary";
        }

        public static class Error
        {
            public const string InvalidParameter = "Parameter is not valid.";
        }
    }
}