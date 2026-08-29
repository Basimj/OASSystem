using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Controllers.Common;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class CrudControllerBase<TKey, TReadDto, TCreateDto, TUpdateDto>(ICrudApplicationService<TKey, TReadDto, TCreateDto, TUpdateDto> service)
    : ControllerBase where TKey : notnull
{
    protected ICrudApplicationService<TKey, TReadDto, TCreateDto, TUpdateDto> Service { get; } = service;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedResult<TReadDto>> GetPage([FromQuery] PageRequest request, CancellationToken cancellationToken) => Service.GetPageAsync(request, cancellationToken);

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<TReadDto> GetById(TKey id, CancellationToken cancellationToken) => Service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TReadDto>> Create([FromBody] TCreateDto request, CancellationToken cancellationToken)
    {
        var result = await Service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<TReadDto> Update(TKey id, [FromBody] TUpdateDto request, CancellationToken cancellationToken) => Service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(TKey id, CancellationToken cancellationToken)
    {
        await Service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
