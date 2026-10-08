using LearnAPI.Application.Services;
using LearnAPI.Common;

namespace LearnAPI.Tests;

public class DocumentServiceTests
{
    private static readonly byte[] PdfBytes = "%PDF-1.4 minimal"u8.ToArray();

    private static (DocumentService Svc, FakeJsonDb Db, FakeStorage Storage) Build(long max = 1024 * 1024)
    {
        var db = new FakeJsonDb();
        db.Handlers["fn_document_register"] = _ => """{"id":1}""";
        var storage = new FakeStorage();
        return (new DocumentService(db, storage, Make.Storage(max)), db, storage);
    }

    private static Task<string> Upload(DocumentService s, Microsoft.AspNetCore.Http.IFormFile f) =>
        s.UploadAsync("policy", 21, "POLICY_COPY", null, f, "tester", CancellationToken.None);

    [Fact]
    public async Task Valid_pdf_is_stored_under_a_generated_name_and_registered()
    {
        var (svc, db, storage) = Build();
        await Upload(svc, Make.File("../../evil name.pdf", PdfBytes));

        var key = Assert.Single(storage.Files.Keys);
        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain("evil", key);                 // the uploaded name is never used as a path
        Assert.DoesNotContain("..", key);
        var call = Assert.Single(db.Calls);
        Assert.Equal("fn_document_register", call.Function);
        Assert.Equal("evil name.pdf", call.Args[3]);        // only the file-name part is kept as a display name
        Assert.Equal(64, ((string)call.Args[7]!).Length);   // SHA-256 hex
    }

    [Fact]
    public async Task Disallowed_extension_is_rejected()
    {
        var (svc, _, storage) = Build();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Upload(svc, Make.File("run.exe", "MZ")));
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task Content_must_match_the_extension()
    {
        var (svc, _, storage) = Build();
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Upload(svc, Make.File("fake.png", "this is text, not a png")));
        Assert.Contains("does not match", ex.Message);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task Empty_and_oversized_files_are_rejected()
    {
        var (svc, _, _) = Build(max: 10);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Upload(svc, Make.File("a.pdf", Array.Empty<byte>())));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Upload(svc, Make.File("big.pdf", PdfBytes)));
    }

    [Fact]
    public async Task Orphan_file_is_removed_when_registration_fails()
    {
        var (svc, db, storage) = Build();
        db.Handlers["fn_document_register"] = _ => throw new InvalidOperationException("db down");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Upload(svc, Make.File("a.pdf", PdfBytes)));
        Assert.Empty(storage.Files);
    }
}
