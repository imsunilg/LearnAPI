using System.Security.Cryptography;
using LearnAPI.Application.Interfaces;
using LearnAPI.Common;
using LearnAPI.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace LearnAPI.Application.Services;

/// <summary>
/// Upload pipeline: validate (size, extension, REAL content signature) → hash → store under a generated name → register metadata.
/// The uploaded file name is never used as a path; it is only kept as a display name.
/// </summary>
public sealed class DocumentService(IJsonDb db, IFileStorage storage, IOptions<StorageOptions> options)
{
    // extension → (content type, magic-byte check). The browser-sent Content-Type is not trusted.
    private static readonly Dictionary<string, (string Mime, Func<byte[], bool> Sig)> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ("application/pdf", h => Starts(h, 0x25, 0x50, 0x44, 0x46)),
        [".png"] = ("image/png", h => Starts(h, 0x89, 0x50, 0x4E, 0x47)),
        [".jpg"] = ("image/jpeg", h => Starts(h, 0xFF, 0xD8, 0xFF)),
        [".jpeg"] = ("image/jpeg", h => Starts(h, 0xFF, 0xD8, 0xFF)),
        [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", h => Starts(h, 0x50, 0x4B, 0x03, 0x04)),
        [".txt"] = ("text/plain", h => !h.Contains((byte)0)),
    };

    public static string AllowedExtensionsText => string.Join(", ", Allowed.Keys);

    private static bool Starts(byte[] h, params byte[] sig) => h.Length >= sig.Length && h.AsSpan(0, sig.Length).SequenceEqual(sig);

    public async Task<string> UploadAsync(string entityType, long entityId, string documentType, string? description, IFormFile file, string user, CancellationToken ct)
    {
        if (file.Length == 0) throw new BusinessRuleException("The file is empty.");
        if (file.Length > options.Value.MaxFileBytes) throw new BusinessRuleException($"The file is larger than {options.Value.MaxFileBytes / (1024 * 1024)} MB.");

        var displayName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(displayName);
        if (!Allowed.TryGetValue(ext, out var rule)) throw new BusinessRuleException($"File type '{ext}' is not allowed. Allowed: {AllowedExtensionsText}.");

        await using var input = file.OpenReadStream();
        var head = new byte[8];
        var read = await input.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
        if (!rule.Sig(head[..read])) throw new BusinessRuleException("The file content does not match its extension.");
        input.Position = 0;

        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(input, ct)).ToLowerInvariant();
        input.Position = 0;

        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        await storage.SaveAsync(key, input, ct);
        try
        {
            return (await db.CallAsync("fn_document_register", ct, entityType, entityId, documentType, displayName, rule.Mime, file.Length, key, checksum, description, user))!;
        }
        catch
        {
            storage.Delete(key); // do not leave an orphan file when the metadata insert fails
            throw;
        }
    }

    public Stream? Open(string key) => storage.Open(key);
}
