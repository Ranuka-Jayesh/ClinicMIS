namespace ClinicMIS.Models.Entities;

/// <summary>
/// Staff role types in the clinic
/// </summary>
public enum StaffRole
{
    Admin = 1,
    Doctor = 2,
    Nurse = 3,
    Pharmacist = 4,
    Receptionist = 5
}

/// <summary>
/// Visit status tracking
/// </summary>
public enum VisitStatus
{
    Scheduled = 1,
    CheckedIn = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

/// <summary>
/// Prescription status for pharmacy workflow
/// </summary>
public enum PrescriptionStatus
{
    Draft = 1,
    SentToPharmacy = 2,
    Processing = 3,
    ReadyForPickup = 4,
    Dispensed = 5,
    Cancelled = 6
}

/// <summary>
/// Billing/Payment status
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4,
    Waived = 5
}

/// <summary>
/// Payment methods accepted
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    Insurance = 4,
    BankTransfer = 5
}

/// <summary>
/// Audit log action types
/// </summary>
public enum AuditAction
{
    Create = 1,
    Read = 2,
    Update = 3,
    Delete = 4,
    Login = 5,
    Logout = 6,
    PasswordChange = 7,
    Export = 8
}
