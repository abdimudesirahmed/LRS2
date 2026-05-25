using Microsoft.AspNetCore.Http;

namespace LRS.Application.Interfaces;

public interface IAlfrescoService
{
    // Uploads file to Alfresco and returns the Node ID
    Task<string> UploadDocumentAsync(IFormFile file, string applicationId, string parcelId);
    
    // Gets the file stream from Alfresco using Node ID
    Task<(Stream Stream, string ContentType)> GetDocumentStreamAsync(string nodeId);

    // Tests connectivity to Alfresco and attempts to resolve the root node id
    Task<(bool Success, string? NodeId, string? ErrorMessage)> TestConnectionAsync();

    // Deletes a node in Alfresco (used for cleanup when DB operations fail)
    Task DeleteNodeAsync(string nodeId);

    // Renames a node in Alfresco
    Task RenameNodeAsync(string nodeId, string newName);
}
