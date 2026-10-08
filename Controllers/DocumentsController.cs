using System.Text.Json.Nodes;
using LearnAPI.Application.Interfaces;
using LearnAPI.Application.Services;
using LearnAPI.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/documents")]
public class DocumentsController(ICrudService service, DocumentService documents) : CrudControllerBase(service)
{
    protected override string Entity => "document";
    public override string PermissionModule => "documents";

    /// <summary>Upload a file (multipart/form-data: file, entityType, entityId, documentType, description).</summary>
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request, CancellationToken ct)
    {
        var json = await documents.UploadAsync(request.EntityType.ToLowerInvariant(), request.EntityId, request.DocumentType.ToUpperInvariant(), request.Description, request.File, CurrentUser, ct);
        return JsonResult(json, StatusCodes.Status201Created);
    }

    /// <summary>Download (attachment) or preview (?inline=true) a stored file.</summary>
    [HttpGet("{id:long}/content")]
    [HasPermission("documents.view")]
    public async Task<IActionResult> Content(long id, [FromQuery] bool inline, CancellationToken ct)
    {
        var json = await Service.GetByIdAsync(Entity, id, ct);
        if (json is null) return NotFoundProblem(id);
        var doc = JsonNode.Parse(json)!;
        var stream = documents.Open(doc["storageKey"]!.GetValue<string>());
        if (stream is null) return NotFound(new ProblemDetails { Status = 404, Title = "File missing", Detail = "The file is no longer in storage." });

        Response.Headers.XContentTypeOptions = "nosniff"; // never let the browser guess a different type
        var name = doc["fileName"]!.GetValue<string>();
        var type = doc["contentType"]!.GetValue<string>();
        // Only types that are safe to render in the page may be shown inline; everything else is forced to download.
        var canInline = inline && type is "application/pdf" or "image/png" or "image/jpeg" or "text/plain";
        return canInline ? File(stream, type) : File(stream, type, name);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct) => JsonResult(await Service.DeleteAsync(Entity, id, CurrentUser, ct));
}

public sealed class UploadDocumentRequest
{
    public IFormFile File { get; set; } = default!;
    public string EntityType { get; set; } = "";
    public long EntityId { get; set; }
    public string DocumentType { get; set; } = "";
    public string? Description { get; set; }
}
