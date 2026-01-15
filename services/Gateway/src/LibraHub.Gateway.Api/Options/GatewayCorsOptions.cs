using System.ComponentModel.DataAnnotations;

namespace LibraHub.Gateway.Api.Options;

public class GatewayCorsOptions
{
    public const string SectionName = "Cors";

    [MinLength(1)]
    public List<string> AllowedOrigins { get; set; } = ["http://localhost:3000"];
}


