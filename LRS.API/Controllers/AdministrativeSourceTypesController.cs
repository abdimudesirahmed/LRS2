using LRS.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LRS.API.Controllers;

[ApiController]
[Route("api/administrative-source-types")]
public class AdministrativeSourceTypesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdministrativeSourceTypesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get all administrative source types (for dropdown/lookup)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _unitOfWork.AdministrativeSourceTypes.GetAllAsync();
        return Ok(types);
    }

    /// <summary>
    /// Get administrative source type by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var type = await _unitOfWork.AdministrativeSourceTypes.GetByIdAsync(id);
        if (type == null)
            return NotFound();
        
        return Ok(type);
    }
}












