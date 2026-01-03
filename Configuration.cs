using Incubation.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Incubation {
    public static class Configuration {
        internal const long OrdersToGenerate = 300_000;

        internal const int MaximumNumberOfCustomers = 100_000;
        internal const int StartCustomersAt = 100;

        internal static readonly char[] CharactersToUse = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
        internal const int MaximumLengthOfSku = 2;

        internal const int MaximumSkuQuantity = 10;
        internal const int MaximumCartItemQuantity = 5;

        internal const string AvailableCharsForRandomEmailPrefixes = "abcdefghijklmnopqrstuvwxyz0123456789";
        internal const string AvailableDigitsForPhoneNumbers = "0123456789";

        internal static readonly string[] RandomEmailSuffixes = [
            "@yahoo.ru",
            "@outlook.com",
            "@hotmail.co.uk",
            "@gmail.com",
            "@monarchy.gov",
            "@qq.com"
        ];
    }
}
