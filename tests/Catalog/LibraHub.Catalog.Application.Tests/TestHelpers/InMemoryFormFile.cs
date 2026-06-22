using Microsoft.AspNetCore.Http;

namespace LibraHub.Catalog.Application.Tests.TestHelpers;

public sealed class InMemoryFormFile : IFormFile
{
    private readonly byte[] _content;

    public InMemoryFormFile(byte[] content, string contentType = "image/png", string fileName = "image.png")
    {
        _content = content;
        ContentType = contentType;
        FileName = fileName;
        Name = "file";
        Headers = new HeaderDictionary();
    }

    public string ContentType { get; set; }
    public string ContentDisposition { get; set; } = string.Empty;
    public IHeaderDictionary Headers { get; set; }
    public long Length => _content.Length;
    public string Name { get; set; }
    public string FileName { get; set; }

    public Stream OpenReadStream() => new MemoryStream(_content, writable: false);

    public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        => target.WriteAsync(_content, 0, _content.Length, cancellationToken);
}
