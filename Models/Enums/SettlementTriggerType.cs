namespace SimplexLawFirm.Models
{
    /// <summary>
    /// What triggered the settlement evaluation.
    /// </summary>
    public enum SettlementTriggerType
    {
        /// <summary>Scheduled periodic evaluation.</summary>
        PeriodicEvaluation = 0,

        /// <summary>A cost estimate was revised.</summary>
        RevisedCostEstimate = 1,

        /// <summary>The likelihood-of-success forecast changed.</summary>
        ForecastChange = 2,

        /// <summary>A new precedent was indexed that affects comparable matters.</summary>
        NewPrecedent = 3,

        /// <summary>Manual trigger by an attorney or director.</summary>
        ManualRequest = 4
    }
}
