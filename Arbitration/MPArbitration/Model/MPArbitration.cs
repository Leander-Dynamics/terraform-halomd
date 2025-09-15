using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MPArbitration.Model
{
    public enum ArbitrationResult
    {
        None = 0,
        CaseClosed,
        HealthPlanWins,
        Ineligible,
        ProviderWins,
        SplitAward
    }

    public enum ArbitrationStatus 
    {
        New,
        Open, 
        InformalInProgress,
        SettledInformalPendingPayment,
        PendingArbitration, 
        ActiveArbitrationBriefNeeded,
        ActiveArbitrationBriefCreated,
        ActiveArbitrationBriefSubmitted,
        SettledArbitrationHealthPlanWon,
        SettledArbitrationNoDecision,
        SettledArbitrationPendingPayment,
        SettledOutsidePendingPayment,
        ClosedPaymentReceived,
        ClosedPaymentWithdrawn,
        Ineligible,
        DetermineAuthority,
        MissingInformation,
        Search = 99,
        Unknown = 100
    }

    public enum CaseDocumentType
    {
        ArbitratorFeeReceipt,
        ArbitrationResolutionReport,
        Brief,
        Check,
        ConsentForm,
        Correspondence,
        CV,
        DeterminationLetter,
        EOB,
        Facesheet,
        HCFA,
        IDCard,
        InsuranceCard,
        InformalNegotiationRequest,
        NegotiationAgreement,
        NinetyDayCoolingOffProof,
        NSARequestAttachment,
        OPReport,
        PaymentConfirmation,
        ProofOfContractNegotiation,
        ProofOfIDRInitiation,
        ProofOfOpenNegotiation,
        ProofOfIDRFeePayment,
        ProviderApplication,
        QPAWhitepaper,
        RepresentativeEOB,
        VOB       
    }

    public enum DeadlineType
    {
        CalendarDays,
        WorkDays
    }

    public enum ArbitratorType
    {
        Arbitrator,
        CertifiedEntity,
        Facilitator,
        Mediator
    }

    /// <summary>
    /// Useful for determining where to send the fee payment or request a refund
    /// </summary>
    public enum FeeRecipient
    {
        Arbitrator,
        Authority
    }

    public enum FeeType
    {
        Administrative,
        BatchSize,
        ClaimSize,
        PerItem,
        PerClaim
    }

    public enum NotificationType
    {
        NSANegotiationRequest = 0,
        NSANegotiationRequestAttachment = 1,
        StateBrief = 2,
        StateBriefComponent = 3,
        IDRBrief = 4,
        IDRBriefComponent = 5,
        Other = 98,
        Unknown = 99
    }

    public enum PlanType
    {
        FullyInsured,
        SelfFunded,
        SelfFundedOptIn
    }
}