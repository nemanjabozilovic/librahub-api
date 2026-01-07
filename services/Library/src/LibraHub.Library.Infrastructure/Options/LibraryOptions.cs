using System.ComponentModel.DataAnnotations;

namespace LibraHub.Library.Infrastructure.Options;

public class LibraryOptions
{
    public const string SectionName = "Library";

    [Required(ErrorMessage = "ConnectionString is required")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMq configuration is required")]
    public RabbitMqOptions RabbitMq { get; set; } = null!;

    [Required(ErrorMessage = "IdentityApiUrl is required")]
    public string IdentityApiUrl { get; set; } = string.Empty;
}

public class RabbitMqOptions
{
    [Required(ErrorMessage = "RabbitMq HostName is required")]
    public string HostName { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "RabbitMq Port must be between 1 and 65535")]
    public int Port { get; set; }

    [Required(ErrorMessage = "RabbitMq UserName is required")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMq Password is required")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "RabbitMq ExchangeName is required")]
    public string ExchangeName { get; set; } = string.Empty;
}
