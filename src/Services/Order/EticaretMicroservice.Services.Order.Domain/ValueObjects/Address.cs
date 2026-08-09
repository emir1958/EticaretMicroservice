using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Domain.ValueObjects
{
    public class Address
    {
        public string City { get; private set; }
        public string District { get; private set; }
        public string Street { get; private set; }
        public string ZipCode { get; private set; }
        public string Line { get; private set; }

        public Address(string city, string district, string street, string zipCode, string line)
        {
            City = city;
            District = district;
            Street = street;
            ZipCode = zipCode;
            Line = line;
        }
    }
}
