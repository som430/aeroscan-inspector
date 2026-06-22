using AeroScan.API.DTOs;
using AeroScan.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AeroScan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InspectionsController : ControllerBase
{
	private readonly InspectionService _service;

	public InspectionsController(InspectionService service) => _service = service;

	// GET /api/inspections
	[HttpGet]
	[ProducesResponseType(typeof(List<SessionSummaryDto>), 200)]
	public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50)
		=> Ok(await _service.GetAllSessionsAsync(skip, take));

	// GET /api/inspections/{id}
	[HttpGet("{id:int}")]
	[ProducesResponseType(typeof(SessionDetailDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetById(int id)
	{
		var dto = await _service.GetSessionDetailAsync(id);
		return dto is null ? NotFound() : Ok(dto);
	}

	// POST /api/inspections
	[HttpPost]
	[ProducesResponseType(typeof(SessionSummaryDto), 201)]
	public async Task<IActionResult> Create([FromBody] CreateSessionRequest req)
	{
		var session = await _service.CreateSessionAsync(req);
		return CreatedAtAction(nameof(GetById), new { id = session.Id }, new SessionSummaryDto
		{
			Id = session.Id,
			PartName = session.PartName,
			PartNumber = session.PartNumber,
			ToleranceMm = session.ToleranceMm,
			Status = session.Status,
			CreatedAt = session.CreatedAt
		});
	}

	// POST /api/inspections/{id}/upload
	[HttpPost("{id:int}/upload")]
	[ProducesResponseType(typeof(InspectionResultDto), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	[RequestSizeLimit(100 * 1024 * 1024)]
	public async Task<IActionResult> Upload(int id, IFormFile file)
	{
		if (file is null || file.Length == 0)
			return BadRequest("파일이 없습니다.");

		var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (ext is not (".csv" or ".ply"))
			return BadRequest(".csv 또는 .ply 파일만 지원합니다.");

		try
		{
			await using var stream = file.OpenReadStream();
			var result = await _service.RunInspectionAsync(id, stream, file.FileName);
			return Ok(result);
		}
		catch (KeyNotFoundException)
		{
			return NotFound();
		}
		catch (NotSupportedException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(ex.Message);
		}
	}

	// DELETE /api/inspections/{id}B
	[HttpDelete("{id:int}")]
	[ProducesResponseType(204)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> Delete(int id)
	{
		var deleted = await _service.DeleteSessionAsync(id);
		return deleted ? NoContent() : NotFound();
	}
}