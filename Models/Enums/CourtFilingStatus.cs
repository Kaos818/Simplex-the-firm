namespace SimplexLawFirm.Models
{
    /// <summary>Lifecycle of a document submitted to the court filing queue.</summary>
    public enum CourtFilingStatus
    {
        PendingValidation = 0,
        ReturnedToAttorney = 1,
        Validated = 2,
        Filed = 3,
        Withdrawn = 4
    }
}
