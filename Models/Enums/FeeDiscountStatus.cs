namespace SimplexLawFirm.Models.Enums
{
    /// <summary>
    /// Lifecycle states for a fee-discount request (UC-05).
    /// Stored as int in [FeeDiscountRequests].[Status].
    /// </summary>
    public enum FeeDiscountStatus
    {
        /// <summary>Request submitted by client; awaiting system evaluation.</summary>
        Pending = 0,

        /// <summary>System auto-approved (within delegated limit + good payment history).</summary>
        AutoApproved = 1,

        /// <summary>Exceeds delegated limit or poor history – routed to Director.</summary>
        PendingDirectorReview = 2,

        /// <summary>Director approved the discount.</summary>
        DirectorApproved = 3,

        /// <summary>Director rejected the discount.</summary>
        DirectorRejected = 4,

        /// <summary>Director issued a counter-offer; awaiting client response.</summary>
        CounterOffered = 5,

        /// <summary>Client accepted the Director's counter-offer.</summary>
        CounterOfferAccepted = 6,

        /// <summary>Client rejected the Director's counter-offer.</summary>
        CounterOfferRejected = 7
    }
}
