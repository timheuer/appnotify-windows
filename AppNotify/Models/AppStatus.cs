using Microsoft.UI;
using Windows.UI;

namespace AppNotify.Models;

public enum AppStatus
{
    PrepareForSubmission,
    WaitingForReview,
    InReview,
    PendingDeveloperRelease,
    PendingAppleRelease,
    ProcessingForDistribution,
    ReadyForDistribution,
    Accepted,
    DeveloperRejected,
    Rejected,
    MetadataRejected,
    InvalidBinary,
    RemovedFromSale,
    DeveloperRemovedFromSale,
    PendingContract,
    ReplacedWithNewVersion,
    Unknown
}

public static class AppStatusExtensions
{
    private static readonly Dictionary<string, AppStatus> _apiMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PREPARE_FOR_SUBMISSION"] = AppStatus.PrepareForSubmission,
        ["WAITING_FOR_REVIEW"] = AppStatus.WaitingForReview,
        ["IN_REVIEW"] = AppStatus.InReview,
        ["PENDING_DEVELOPER_RELEASE"] = AppStatus.PendingDeveloperRelease,
        ["PENDING_APPLE_RELEASE"] = AppStatus.PendingAppleRelease,
        ["PROCESSING_FOR_DISTRIBUTION"] = AppStatus.ProcessingForDistribution,
        ["READY_FOR_DISTRIBUTION"] = AppStatus.ReadyForDistribution,
        ["ACCEPTED"] = AppStatus.Accepted,
        ["DEVELOPER_REJECTED"] = AppStatus.DeveloperRejected,
        ["REJECTED"] = AppStatus.Rejected,
        ["METADATA_REJECTED"] = AppStatus.MetadataRejected,
        ["INVALID_BINARY"] = AppStatus.InvalidBinary,
        ["REMOVED_FROM_SALE"] = AppStatus.RemovedFromSale,
        ["DEVELOPER_REMOVED_FROM_SALE"] = AppStatus.DeveloperRemovedFromSale,
        ["PENDING_CONTRACT"] = AppStatus.PendingContract,
        ["REPLACED_WITH_NEW_VERSION"] = AppStatus.ReplacedWithNewVersion,
    };

    public static AppStatus FromApiString(string? value) =>
        value is not null && _apiMap.TryGetValue(value, out var status) ? status : AppStatus.Unknown;

    public static string DisplayName(this AppStatus status) => status switch
    {
        AppStatus.PrepareForSubmission => "Prepare for Submission",
        AppStatus.WaitingForReview => "Waiting for Review",
        AppStatus.InReview => "In Review",
        AppStatus.PendingDeveloperRelease => "Pending Developer Release",
        AppStatus.ReadyForDistribution => "Ready for Distribution",
        AppStatus.ProcessingForDistribution => "Processing",
        AppStatus.DeveloperRejected => "Developer Rejected",
        AppStatus.Rejected => "Rejected",
        AppStatus.MetadataRejected => "Metadata Rejected",
        AppStatus.RemovedFromSale => "Removed from Sale",
        AppStatus.DeveloperRemovedFromSale => "Developer Removed",
        AppStatus.InvalidBinary => "Invalid Binary",
        AppStatus.PendingAppleRelease => "Pending Apple Release",
        AppStatus.PendingContract => "Pending Contract",
        AppStatus.ReplacedWithNewVersion => "Replaced",
        AppStatus.Accepted => "On the App Store",
        _ => "Unknown"
    };

    public static int SortOrder(this AppStatus status) => status switch
    {
        AppStatus.Rejected or AppStatus.MetadataRejected or AppStatus.InvalidBinary => 0,
        AppStatus.DeveloperRejected => 1,
        AppStatus.InReview => 2,
        AppStatus.WaitingForReview => 3,
        AppStatus.PrepareForSubmission => 4,
        AppStatus.ProcessingForDistribution => 5,
        AppStatus.PendingDeveloperRelease => 6,
        AppStatus.PendingAppleRelease => 7,
        AppStatus.PendingContract => 8,
        AppStatus.ReadyForDistribution or AppStatus.Accepted => 9,
        AppStatus.RemovedFromSale or AppStatus.DeveloperRemovedFromSale => 10,
        AppStatus.ReplacedWithNewVersion => 11,
        _ => 12
    };

    public static Color StatusColor(this AppStatus status) => status switch
    {
        AppStatus.ReadyForDistribution or AppStatus.Accepted => ColorHelper.FromArgb(255, 16, 185, 129),
        AppStatus.PendingDeveloperRelease => ColorHelper.FromArgb(255, 45, 212, 191),
        AppStatus.InReview or AppStatus.WaitingForReview => ColorHelper.FromArgb(255, 245, 158, 11),
        AppStatus.PrepareForSubmission => ColorHelper.FromArgb(255, 59, 130, 246),
        AppStatus.ProcessingForDistribution => ColorHelper.FromArgb(255, 139, 92, 246),
        AppStatus.Rejected or AppStatus.MetadataRejected or AppStatus.InvalidBinary => ColorHelper.FromArgb(255, 239, 68, 68),
        AppStatus.DeveloperRejected => ColorHelper.FromArgb(255, 236, 72, 153),
        AppStatus.RemovedFromSale or AppStatus.DeveloperRemovedFromSale => ColorHelper.FromArgb(255, 156, 163, 175),
        _ => ColorHelper.FromArgb(255, 156, 163, 175),
    };
}
