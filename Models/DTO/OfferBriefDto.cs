using System;
using System.Collections.Generic;

namespace IME.MobileApp.Api.Models
{
    public class OfferBriefDto
    {
        public int Id { get; set; }
        public string OfferSymbol { get; set; }
        public string SupplierName { get; set; }
        public string OfferDate { get; set; }
        public string TradeStatus { get; set; }
    }
}

