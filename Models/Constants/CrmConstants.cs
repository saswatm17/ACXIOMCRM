namespace AcxiomCRM.Models.Constants
{
    // Application user roles
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "SalesExecutive";
    }

    // Customer status constants
    public static class CustomerStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
    }

    // Lead status workflow constants
    public static class LeadStatus
    {
        public const string New = "New";
        public const string Contacted = "Contacted";
        public const string Qualified = "Qualified";
        public const string Unqualified = "Unqualified";
        public const string Converted = "Converted";
        public const string Lost = "Lost";

        public static readonly string[] All = { New, Contacted, Qualified, Unqualified, Converted, Lost };
    }

    // Lead source options
    public static class LeadSource
    {
        public const string Website = "Website";
        public const string Referral = "Referral";
        public const string ColdCall = "Cold Call";
        public const string SocialMedia = "Social Media";
        public const string EmailCampaign = "Email Campaign";
        public const string Event = "Event";

        public static readonly string[] All = { Website, Referral, ColdCall, SocialMedia, EmailCampaign, Event };
    }

    // Opportunity pipeline stages
    public static class OpportunityStage
    {
        public const string Qualification = "Qualification";
        public const string Proposal = "Proposal";
        public const string Negotiation = "Negotiation";
        public const string Won = "Won";
        public const string Lost = "Lost";

        public static readonly string[] All = { Qualification, Proposal, Negotiation, Won, Lost };
    }

    // Opportunity overall status
    public static class OpportunityStatus
    {
        public const string Open = "Open";
        public const string Won = "Won";
        public const string Lost = "Lost";
        public const string Closed = "Closed";

        public static readonly string[] All = { Open, Won, Lost, Closed };
    }

    // Follow-up status constants
    public static class FollowUpStatus
    {
        public const string Planned = "Planned";
        public const string Completed = "Completed";
        public const string Missed = "Missed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Planned, Completed, Missed, Cancelled };
    }

    // Follow-up types
    public static class FollowUpType
    {
        public const string Call = "Call";
        public const string Meeting = "Meeting";
        public const string Email = "Email";
        public const string ProductDemo = "Product Demo";

        public static readonly string[] All = { Call, Meeting, Email, ProductDemo };
    }

    // Activity types
    public static class ActivityType
    {
        public const string Call = "Call";
        public const string Meeting = "Meeting";
        public const string Email = "Email";
        public const string Task = "Task";

        public static readonly string[] All = { Call, Meeting, Email, Task };
    }

    // Activity statuses
    public static class ActivityStatus
    {
        public const string Pending = "Pending";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Completed, Cancelled };
    }

    // Audit action types
    public static class AuditAction
    {
        public const string Login = "Login";
        public const string FailedLogin = "FailedLogin";
        public const string Logout = "Logout";
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string RoleChange = "RoleChange";
        public const string PasswordChange = "PasswordChange";
        public const string LeadConversion = "LeadConversion";
    }
}
