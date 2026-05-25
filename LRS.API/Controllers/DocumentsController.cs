using LRS.Application.DTOs;
using LRS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;

namespace LRS.API.Controllers;

[Authorize]
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>
    /// Upload a document (SRS User Story 1: Scan and Upload Document)
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto dto)
    {
        // Validation is handled by model binding and data annotations
        // Global exception handler will catch and format exceptions
        var result = await _documentService.UploadDocumentAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Get document file (SRS User Story 2: View Document)
    /// File is streamed from Alfresco through API (never exposed via direct Alfresco URL)
    /// This endpoint returns the file inline where possible (no forced download) so it can be opened in a new tab for viewing.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var (stream, contentType, fileName) = await _documentService.GetDocumentFileAsync(id);
        // Do not provide fileDownloadName so the browser will attempt to display inline when it can
        return File(stream, contentType);
    }

    /// <summary>
    /// Download document file (forces attachment/download)
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        var (stream, contentType, fileName) = await _documentService.GetDocumentFileAsync(id);
        // Providing fileDownloadName will set Content-Disposition: attachment and force download in browsers
        return File(stream, contentType, fileName);
    }
    
    [HttpGet("source/{sourceId}")]
    public async Task<IActionResult> GetDocumentsBySource(int sourceId)
    {
        var docs = await _documentService.GetDocumentsBySourceIdAsync(sourceId);
        return Ok(docs);
    }

    /// <summary>
    /// Checks if a duplicate document exists for a given parcel ID and administrative source type.
    /// </summary>
    [HttpGet("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string parcelId, [FromQuery] int adminSourceTypeId)
    {
        if (string.IsNullOrEmpty(parcelId) || adminSourceTypeId <= 0)
        {
            return BadRequest("Invalid parcel ID or administrative source type ID.");
        }
        var result = await _documentService.GetLatestByParcelAndTypeAsync(parcelId, adminSourceTypeId);
        return Ok(result);
    }
}
