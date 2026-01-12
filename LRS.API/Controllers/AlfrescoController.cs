using LRS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LRS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlfrescoController : ControllerBase
{
 private readonly IAlfrescoService _alfrescoService;
 private readonly ILogger<AlfrescoController> _logger;

 public AlfrescoController(IAlfrescoService alfrescoService, ILogger<AlfrescoController> logger)
 {
 _alfrescoService = alfrescoService;
 _logger = logger;
 }

 [HttpGet("test")]
 public async Task<IActionResult> Test()
 {
 var (success, nodeId, error) = await _alfrescoService.TestConnectionAsync();
 if (success) return Ok(new { success = true, nodeId });
 return StatusCode(503, new { success = false, error });
 }
}
