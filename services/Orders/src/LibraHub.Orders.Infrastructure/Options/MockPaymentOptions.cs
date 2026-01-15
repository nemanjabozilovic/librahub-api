using System.ComponentModel.DataAnnotations;

namespace LibraHub.Orders.Infrastructure.Options;

public class MockPaymentOptions
{
    public const string SectionName = "MockPayment";

    [Range(0, 100, ErrorMessage = "FailureProbabilityPercent must be between 0 and 100")]
    public int FailureProbabilityPercent { get; set; }

    public bool UseAmountBasedFailure { get; set; }

    [Required(ErrorMessage = "FailureAmountEndings list is required")]
    public List<string> FailureAmountEndings { get; set; } = null!;

    [Required(ErrorMessage = "FailureReasons list is required and cannot be empty")]
    [MinLength(1, ErrorMessage = "At least one failure reason must be provided")]
    public List<string> FailureReasons { get; set; } = null!;
}
