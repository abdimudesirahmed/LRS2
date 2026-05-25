using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LRS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LRS.Infrastructure.Services;

public class AlfrescoService : IAlfrescoService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlfrescoService> _logger;
    private readonly string _baseUrl;
    private readonly string _rootFolderId;

    private string? _cachedRootNodeId;

    public AlfrescoService(HttpClient httpClient, IConfiguration configuration, ILogger<AlfrescoService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _baseUrl = _configuration["Alfresco:BaseUrl"]?.TrimEnd('/') 
                   ?? throw new ArgumentNullException("Alfresco:BaseUrl is not configured");
        _rootFolderId = _configuration["Alfresco:RootFolderId"] ?? "-root-";

        var username = _configuration["Alfresco:Username"];
        var password = _configuration["Alfresco:Password"];

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            
            // Also set Accept header for JSON responses
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        _logger.LogInformation("AlfrescoService initialized. BaseUrl: {BaseUrl}, RootFolderId: {RootFolderId}", 
            _baseUrl, _rootFolderId);
    }

    private async Task<string> GetRootNodeIdAsync()
    {
        if (!string.IsNullOrEmpty(_cachedRootNodeId))
            return _cachedRootNodeId;

        // Try different approaches to get the root node
        // Method 1: Try using the configured alias directly
        try
        {
            var url = $"{_baseUrl}/nodes/{_rootFolderId}";
            _logger.LogInformation("Attempting to get root node from: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var contentPreview = string.IsNullOrEmpty(responseContent) 
                ? "(empty)" 
                : responseContent.Substring(0, Math.Min(200, responseContent.Length));
            _logger.LogInformation("Root node request response: Status={Status}, Content={Content}", 
                response.StatusCode, contentPreview);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonNode>();
                var nodeId = content?["entry"]?["id"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(nodeId))
                {
                    _cachedRootNodeId = nodeId;
                    _logger.LogInformation("Root node ID resolved: {NodeId}", nodeId);
                    return nodeId;
                }
            }
            else
            {
                _logger.LogWarning("Failed to get root node. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get root node using -root- alias");
        }

        // Method 2: Try getting root via Company Home
        try
        {
            var url = $"{_baseUrl}/nodes/-root-?relativePath=Company Home";
            _logger.LogInformation("Attempting to get root node via Company Home: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonNode>();
                var nodeId = content?["entry"]?["id"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(nodeId))
                {
                    _cachedRootNodeId = nodeId;
                    _logger.LogInformation("Root node ID resolved via Company Home: {NodeId}", nodeId);
                    return nodeId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get root node via Company Home");
        }

        // Method 3: Use configured root as fallback (might work for some operations)
        _logger.LogWarning("Could not resolve root node ID, using {RootId} alias as fallback", _rootFolderId);
        return _rootFolderId;
    }

    public async Task<string> UploadDocumentAsync(IFormFile file, string applicationId, string parcelId)
    {
        try
        {
            // Get the actual root node ID
            var rootNodeId = await GetRootNodeIdAsync();
            
            // 1. Ensure Parcel Folder exists under Root
            var parcelFolderId = await EnsureFolderAsync(rootNodeId, parcelId);

            // 2. Ensure Application Folder exists under Parcel
            var appFolderId = await EnsureFolderAsync(parcelFolderId, applicationId);

            // 3. Ensure Application subfolder exists
            var applicationSubFolderId = await EnsureFolderAsync(appFolderId, "Application");

            // 4. Ensure BaUnit subfolder exists
            var baUnitFolderId = await EnsureFolderAsync(applicationSubFolderId, "BaUnit");

            // 5. Ensure SpatialUnit subfolder exists
            var spatialUnitFolderId = await EnsureFolderAsync(baUnitFolderId, "SpatialUnit");

            // 6. Upload File to SpatialUnit folder
            return await UploadFileToFolderAsync(spatialUnitFolderId, file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document to Alfresco. App: {AppId}", applicationId);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType)> GetDocumentStreamAsync(string nodeId)
    {
        var url = $"{_baseUrl}/nodes/{nodeId}/content";
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve document content. NodeId: {NodeId}, Status: {Status}", nodeId, response.StatusCode);
            throw new FileNotFoundException($"Document with NodeId {nodeId} not found in Alfresco or inaccessible.");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();
        return (stream, contentType);
    }

    private async Task<string> EnsureFolderAsync(string parentId, string folderName)
    {
        // Try to find the folder first
        // Using listing with a filter might be heavy if many children, but REST API allows filtering?
        // Simpler: Try to CREATE. If 409, then it exists.
        // BUT, if 409, we need the ID. Validating existence via Create failure doesn't give ID.
        // So Loop:
        // 1. Check if exists (Get Child by Name)
        // 2. If null, Create.
        
        var existingId = await GetChildIdByNameAsync(parentId, folderName);
        if (existingId != null) return existingId;

        return await CreateFolderAsync(parentId, folderName);
    }

    private async Task<string?> GetChildIdByNameAsync(string parentId, string name)
    {
        // Try using relativePath parameter - this works for getting a child node by path
        try
        {
            var url = $"{_baseUrl}/nodes/{parentId}?relativePath={Uri.EscapeDataString(name)}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonNode>();
                var nodeId = content?["entry"]?["id"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(nodeId))
                {
                    return nodeId;
                }
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Error checking folder existence via relativePath. Status: {Status}, Response: {Response}", 
                response.StatusCode, errorContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check folder existence via relativePath");
        }

        // Fallback: List children and find by name (less efficient but more reliable)
        try
        {
            var url = $"{_baseUrl}/nodes/{parentId}/children";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonNode>();
                var entries = content?["list"]?["entries"]?.AsArray();
                
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        var entryNode = entry?["entry"];
                        var entryName = entryNode?["name"]?.GetValue<string>();
                        if (string.Equals(entryName, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return entryNode?["id"]?.GetValue<string>();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check folder existence via children list");
        }

        return null;
    }

    private async Task<string> CreateFolderAsync(string parentId, string folderName
)
    {
        var url = $"{_baseUrl}/nodes/{parentId}/children";
        var payload = new
        {
            name = folderName,
            nodeType = "cm:folder"
        };
        
        var response = await _httpClient.PostAsJsonAsync(url, payload);

        if (response.IsSuccessStatusCode)
        {
             var content = await response.Content.ReadFromJsonAsync<JsonNode>();
             return content?["entry"]?["id"]?.GetValue<string>() 
                    ?? throw new Exception("Created folder but ID was missing");
        }

        // Handle race condition: If it was created between our check and now
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
             var id = await GetChildIdByNameAsync(parentId, folderName);
             return id ?? throw new Exception("Folder conflict reported but could not be found.");
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Failed to create folder. Name: {Name}, ParentId: {ParentId}, Status: {Status}, Response: {Response}", 
            folderName, parentId, response.StatusCode, error);
        throw new Exception($"Failed to create folder '{folderName}': {response.StatusCode} - {error}");
    }

    private async Task<string> UploadFileToFolderAsync(string folderId, IFormFile file)
    {
        var url = $"{_baseUrl}/nodes/{folderId}/children";
        
        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        
        content.Add(streamContent, "filedata", file.FileName);
        
        var response = await _httpClient.PostAsync(url, content);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonNode>();
            return result?["entry"]?["id"]?.GetValue<string>() 
                   ?? throw new Exception("Uploaded file but ID was missing");
        }
        
        // If conflict (file exists), try to find existing child by name and return its id
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Upload conflict for file {FileName} in folder {FolderId}. Attempting to resolve by finding existing node.", file.FileName, folderId);
            var existingId = await GetChildIdByNameAsync(folderId, file.FileName);
            if (!string.IsNullOrEmpty(existingId))
            {
                _logger.LogInformation("Resolved upload conflict by using existing node {NodeId} for file {FileName}", existingId, file.FileName);
                return existingId;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to upload file. FileName: {FileName}, FolderId: {FolderId}, Status: {Status}, Response: {Response}", 
                file.FileName, folderId, response.StatusCode, error);
            throw new Exception($"Failed to upload file '{file.FileName}': {response.StatusCode} - {error}");
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        _logger.LogError("Failed to upload file. FileName: {FileName}, FolderId: {FolderId}, Status: {Status}, Response: {Response}", 
            file.FileName, folderId, response.StatusCode, errorBody);
        throw new Exception($"Failed to upload file '{file.FileName}': {response.StatusCode} - {errorBody}");
    }

    public async Task<(bool Success, string? NodeId, string? ErrorMessage)> TestConnectionAsync()
    {
        try
        {
            var nodeId = await GetRootNodeIdAsync();
            // Make a lightweight request to validate credentials and connectivity
            var url = $"{_baseUrl}/nodes/{nodeId}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return (true, nodeId, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Alfresco test connection failed. Status: {Status}, Response: {Response}", response.StatusCode, content);
            return (false, nodeId, $"Status: {response.StatusCode}. Response: {content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alfresco test connection exception");
            return (false, null, ex.Message);
        }
    }

    public async Task DeleteNodeAsync(string nodeId)
    {
        try
        {
            var url = $"{_baseUrl}/nodes/{nodeId}";
            var response = await _httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to delete Alfresco node {NodeId}. Status: {Status}, Response: {Response}", nodeId, response.StatusCode, error);
            }
            else
            {
                _logger.LogInformation("Deleted Alfresco node {NodeId}", nodeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while trying to delete Alfresco node {NodeId}", nodeId);
        }
    }

    public async Task RenameNodeAsync(string nodeId, string newName)
    {
        try
        {
            var url = $"{_baseUrl}/nodes/{nodeId}";
            var payload = new { name = newName };
            var response = await _httpClient.PutAsJsonAsync(url, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to rename Alfresco node {NodeId}. Status: {Status}, Response: {Response}", nodeId, response.StatusCode, error);
            }
            else
            {
                _logger.LogInformation("Renamed Alfresco node {NodeId} to {NewName}", nodeId, newName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while trying to rename Alfresco node {NodeId}", nodeId);
        }
    }
}
