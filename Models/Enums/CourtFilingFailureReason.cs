namespace SimplexLawFirm.Models
{
    /// <summary>Individual checks performed before a court filing is accepted.</summary>
    public enum CourtFilingFailureReason
    {
        WrongDocumentType = 0,
        CaseNumberMismatch = 1,
        AttorneyNotOfRecord = 2,
        SignatureMissing = 3,
        MatterInactive = 4,
        NoProceduralSequence = 5
    }
}
