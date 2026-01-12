using LRS.Application.DTOs;
using LRS.Application.Interfaces;
using LRS.Domain.Entities;
using LRS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace LRS.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAlfrescoService _alfrescoService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IUnitOfWork unitOfWork, IAlfrescoService alfrescoService, ILogger<DocumentService> logger)
    {
        _unitOfWork = unitOfWork;
        _alfrescoService = alfrescoService;
        _logger = logger;
    }

    public async Task<DocumentResponseDto> UploadDocumentAsync(UploadDocumentDto dto)
    {
        //0. Validate Administrative Source Type
        var adminSourceType = await _unitOfWork.AdministrativeSourceTypes.GetByIdAsync(dto.AdministrativeSourceTypeId)
         ?? throw new ArgumentException($"Administrative Source Type {dto.AdministrativeSourceTypeId} not found");

        Source source;

        //1. Resolve or create Source BEFORE uploading to Alfresco to avoid orphaned files
        if (dto.SourceId.HasValue && dto.SourceId.Value >0)
        {
            source = await _unitOfWork.Sources.GetByIdAsync(dto.SourceId.Value);
            if (source == null)
            {
                // Source was provided but not found - create a new one instead of failing
                _logger.LogWarning("Provided SourceId {SourceId} not found - creating a new Source", dto.SourceId.Value);
                source = new Source
                {
                    RecordationDate = dto.RecordationDate ?? DateTime.UtcNow,
                    IsCreated = true,
                    Status = true
                };
                await _unitOfWork.Sources.AddAsync(source);
                await _unitOfWork.SaveChangesAsync();

                var adminSource = new AdministrativeSource
                {
                    SourceId = source.Id,
                    AdministrativeSourceTypeId = dto.AdministrativeSourceTypeId
                };
                await _unitOfWork.AdministrativeSources.AddAsync(adminSource);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Ensure administrative source association exists (or create it)
                var adminSources = await _unitOfWork.AdministrativeSources.FindAsync(s => s.SourceId == source.Id);
                var adminSource = adminSources.FirstOrDefault();

                if (adminSource == null)
                {
                    adminSource = new AdministrativeSource
                    {
                        SourceId = source.Id,
                        AdministrativeSourceTypeId = dto.AdministrativeSourceTypeId
                    };
                    await _unitOfWork.AdministrativeSources.AddAsync(adminSource);
                    await _unitOfWork.SaveChangesAsync();
                }
                else if (adminSource.AdministrativeSourceTypeId != dto.AdministrativeSourceTypeId)
                {
                    // Instead of rejecting, allow multiple types for same Source by creating another association
                    _logger.LogWarning("Source {SourceId} already has AdministrativeSourceType {ExistingType}; adding additional type {RequestedType}",
                     source.Id, adminSource.AdministrativeSourceTypeId, dto.AdministrativeSourceTypeId);

                    var additional = new AdministrativeSource
                    {
                        SourceId = source.Id,
                        AdministrativeSourceTypeId = dto.AdministrativeSourceTypeId
                    };
                    await _unitOfWork.AdministrativeSources.AddAsync(additional);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
        else
        {
            // Create new Source
            source = new Source
            {
                RecordationDate = dto.RecordationDate ?? DateTime.UtcNow,
                IsCreated = true,
                Status = true
            };

            await _unitOfWork.Sources.AddAsync(source);
            await _unitOfWork.SaveChangesAsync(); // persist to get Id

            var adminSource = new AdministrativeSource
            {
                SourceId = source.Id,
                AdministrativeSourceTypeId = dto.AdministrativeSourceTypeId
            };
            await _unitOfWork.AdministrativeSources.AddAsync(adminSource);
            await _unitOfWork.SaveChangesAsync();
        }

        //2. Upload file to Alfresco (now that Source is validated/created)
        string? nodeId = null;
        try
        {
            _logger.LogInformation("Uploading file to Alfresco for AppRegId={AppRegId}, File={FileName}", dto.AppRegId, dto.File.FileName);
            nodeId = await _alfrescoService.UploadDocumentAsync(dto.File, dto.AppRegId);
            _logger.LogInformation("Alfresco returned nodeId={NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alfresco upload failed for AppRegId={AppRegId}", dto.AppRegId);
            throw;
        }

        if (string.IsNullOrEmpty(nodeId))
        {
            _logger.LogError("Alfresco did not return a node id for uploaded file. AppRegId={AppRegId}", dto.AppRegId);
            throw new Exception("Alfresco did not return a valid node id for the uploaded file.");
        }

        //3. Handle versioning
        var existingDoc = await _unitOfWork.Documents.GetLatestBySourceAndTypeAsync(source.Id, dto.AdministrativeSourceTypeId);
        if (existingDoc != null)
        {
            existingDoc.IsVoid = true;
            _unitOfWork.Documents.Update(existingDoc);
        }

        //4. Create and save Document metadata
        var document = new Document
        {
            SourceId = source.Id,
            AppRegId = dto.AppRegId,
            UniqueParcelId = string.Empty,
            AlfDocumentId = nodeId,
            SubmissionDate = DateTime.UtcNow,
            CreatedBy = dto.CreatedBy ?? "system",
            DocumentName = adminSourceType.EnglishValue,
            IsVoid = false
        };

        try
        {
            await _unitOfWork.Documents.AddAsync(document);
            _logger.LogInformation("Saving document metadata to database for AppRegId={AppRegId}, NodeId={NodeId}", dto.AppRegId, nodeId);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Document saved to DB with Id={DocumentId}", document.Id);

            return new DocumentResponseDto
            {
                Id = document.Id,
                SourceId = document.SourceId,
                AlfDocumentId = document.AlfDocumentId,
                AppRegId = document.AppRegId,
                UniqueParcelId = document.UniqueParcelId,
                SubmissionDate = document.SubmissionDate,
                IsVoid = document.IsVoid,
                DocumentName = document.DocumentName,
                AdminSourceTypeEnglish = adminSourceType.EnglishValue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document metadata to DB for AppRegId={AppRegId}. Cleaning up Alfresco node {NodeId}", dto.AppRegId, nodeId);
            if (!string.IsNullOrEmpty(nodeId))
            {
                try
                {
                    await _alfrescoService.DeleteNodeAsync(nodeId);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup Alfresco node {NodeId} after DB failure", nodeId);
                }
            }

            throw;
        }
    }

    public async Task<(Stream FileStream, string ContentType, string FileName)> GetDocumentFileAsync(int documentId)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
        if (document == null) throw new FileNotFoundException("Document not found");
        if (string.IsNullOrEmpty(document.AlfDocumentId)) throw new FileNotFoundException("Document has no content ID");

        var stream = await _alfrescoService.GetDocumentStreamAsync(document.AlfDocumentId);

        // Determine content type from file extension when possible
        var fileName = document.DocumentName ?? "document.pdf";
        string contentType = "application/octet-stream";
        try
        {
            // Use ASP.NET Core provider to map extensions to MIME types
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }
        }
        catch
        {
            contentType = "application/octet-stream";
        }

        return (stream, contentType, fileName);
    }

    public async Task<IEnumerable<DocumentResponseDto>> GetDocumentsBySourceIdAsync(int sourceId)
    {
        var docs = await _unitOfWork.Documents.FindAsync(d => d.SourceId == sourceId);

        return docs.Select(d => new DocumentResponseDto
        {
            Id = d.Id,
            SourceId = d.SourceId,
            AlfDocumentId = d.AlfDocumentId,
            AppRegId = d.AppRegId,
            UniqueParcelId = d.UniqueParcelId,
            SubmissionDate = d.SubmissionDate,
            IsVoid = d.IsVoid,
            DocumentName = d.DocumentName
        });
    }
}
