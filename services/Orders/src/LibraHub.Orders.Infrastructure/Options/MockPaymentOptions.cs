using System.ComponentModel.DataAnnotations;

namespace LibraHub.Orders.Infrastructure.Options;

public class MockPaymentOptions
{
    public const string SectionName = "MockPayment";

    /// <summary>
    /// Probability of payment failure (0-100). 0 means always succeed, 100 means always fail.
    /// </summary>
    [Range(0, 100, ErrorMessage = "FailureProbabilityPercent must be between 0 and 100")]
    public int FailureProbabilityPercent { get; set; }

    /// <summary>
    /// If true, payments with amount ending in specific digits will fail (e.g., .99, .98)
    /// </summary>
    public bool UseAmountBasedFailure { get; set; }

    /// <summary>
    /// Amount endings that trigger failure (e.g., ["99", "98"] means amounts ending in .99 or .98 will fail)
    /// </summary>
    [Required(ErrorMessage = "FailureAmountEndings list is required")]
    public List<string> FailureAmountEndings { get; set; } = null!;

    /// <summary>
    /// Failure reasons to randomly select from
    /// </summary>
    [Required(ErrorMessage = "FailureReasons list is required and cannot be empty")]
    [MinLength(1, ErrorMessage = "At least one failure reason must be provided")]
    public List<string> FailureReasons { get; set; } = null!;
}
