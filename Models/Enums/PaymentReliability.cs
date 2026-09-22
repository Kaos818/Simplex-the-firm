namespace SimplexLawFirm.Models.Enums
{
    /// <summary>
    /// Risk classification of a client's payment behaviour.
    /// Stored as nvarchar(40) in [FeeDiscountRequests].[PaymentReliability].
    /// </summary>
    public static class PaymentReliability
    {
        public const string Good = "Good";
        public const string Fair = "Fair";
        public const string Poor = "Poor";
    }
}
