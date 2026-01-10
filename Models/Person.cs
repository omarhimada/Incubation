namespace Incubation.Models {
    internal enum PersonSegment {
        Steady,
        DealSeeker,
        HighValue,
        AtRisk
    }

    internal sealed record Person(
        int PersonId,                    // maps to Customer.CustomerId
        PersonSegment Segment,
        double BaseOrderLambda,          // avg orders per ~year-ish
        double BasketMean,               // mean cart items
        double PriceSensitivity,         // affects qty / sku choice
        double InternationalAffinity,    // affects intl usage + plan
        double SupportNeediness,         // affects service calls
        int PreferredSkuCluster          // taste group for MF structure
    );
}
