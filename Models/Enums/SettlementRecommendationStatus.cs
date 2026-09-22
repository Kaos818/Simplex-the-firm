namespace SimplexLawFirm.Models
{
    /// <summary>
    /// Lifecycle states for a settlement recommendation (UC-06).
    /// </summary>
    public enum SettlementRecommendationStatus
    {
        /// <summary>System generated the recommendation; awaiting attorney review.</summary>
        PendingAttorneyReview = 0,

        /// <summary>Attorney accepted the recommendation.</summary>
        AttorneyAccepted = 1,

        /// <summary>Attorney rejected the recommendation with reason.</summary>
        AttorneyRejected = 2,

        /// <summary>Attorney escalated to Director for final decision.</summary>
        EscalatedToDirector = 3,

        /// <summary>Director approved pursuing settlement.</summary>
        DirectorApproved = 4,

        /// <summary>Director rejected the settlement recommendation.</summary>
        DirectorRejected = 5,

        /// <summary>Settlement is being pursued; matter status updated.</summary>
        SettlementInProgress = 6,

        /// <summary>Recommendation superseded by a newer evaluation.</summary>
        Superseded = 7
    }
}
