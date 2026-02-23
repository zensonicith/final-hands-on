using CsvHelper.Configuration.Attributes;
using System.Text.Json.Serialization;

namespace Handson.Core.Models
{
    public class HousePrice
    {
        [Name("price")]
        public long Price { get; set; }

        [Name("area")]
        public int Area { get; set; }

        [Name("bedrooms")]
        public int Bedrooms { get; set; }

        [Name("furnishingstatus")]
        public required string FurnishingStatus { get; set; }

        [Name("airconditioning")]
        public required string AirConditioning { get; set; }

        [Name("parking")]
        public int Parking { get; set; }
    }
}
