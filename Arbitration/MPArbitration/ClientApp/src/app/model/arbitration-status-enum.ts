export enum ArbitrationResult
{
    None = 0,
    CaseClosed,
    HealthPlanWins,
    Ineligible,
    ProviderWins,
    SplitAward
}

export enum ArbitrationStatus 
{
    Assigned_To_Facilitator
}

export enum CMSCaseStatus 
{
    New, // when Authority info first copied to an ArbitrationCase, user will have to manually assign status (prob can calc this later)
    Open, // = submitted for arbitration(begins informal process)
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
    Search = 99, // status used as a wildcard for searching - should never be saved
    Unknown = 100
}

