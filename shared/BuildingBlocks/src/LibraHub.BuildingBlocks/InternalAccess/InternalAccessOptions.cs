using System.ComponentModel.DataAnnotations;

namespace LibraHub.BuildingBlocks.InternalAccess;

public class InternalAccessOptions
{
    public const string SectionName = "InternalAccess";

    [Required(ErrorMessage = "InternalAccess:ApiKey is required")]
    public string ApiKey { get; set; } = string.Empty;
}
