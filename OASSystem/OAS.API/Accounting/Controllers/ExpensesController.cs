using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OAS.Application.Accounting.Expenses.Commands.CreateExpense;
using OAS.Application.Accounting.Expenses.Commands.SetExpenseStatus;
using OAS.Application.Accounting.Expenses.Commands.UpdateExpense;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.CreateExpenseType;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.SetExpenseTypeStatus;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.UpdateExpenseType;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypeById;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypes;
using OAS.Application.Accounting.Expenses.Queries.GetExpenseById;
using OAS.Application.Accounting.Expenses.Queries.GetExpenses;
using OAS.Contracts.Accounting.Expenses;
using OAS.Contracts.Common.Pagination;

namespace OAS.API.Accounting.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/accounting/expenses")]
public sealed class ExpensesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> Get(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetExpensesQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExpenseByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateExpenseCommand(request), cancellationToken);
        var dto = await sender.Send(new GetExpenseByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseDto>> Update(
        Guid id,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateExpenseCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetExpenseByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ExpenseDto>> SetStatus(
        Guid id,
        [FromBody] SetExpenseStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetExpenseStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetExpenseByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    // --- Expense Types ---

    [HttpGet("types")]
    public async Task<ActionResult<PagedResult<ExpenseTypeDto>>> GetTypes(
        [FromQuery] PageRequest request,
        [FromQuery(Name = "searchTerm")] string? searchTerm,
        CancellationToken cancellationToken)
    {
        request = AccountingPageRequestCompatibility.Apply(request, searchTerm);
        var result = await sender.Send(new GetExpenseTypesQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("types/{id:guid}")]
    public async Task<ActionResult<ExpenseTypeDto>> GetTypeById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetExpenseTypeByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("types")]
    public async Task<ActionResult<ExpenseTypeDto>> CreateType(
        [FromBody] CreateExpenseTypeRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await sender.Send(new CreateExpenseTypeCommand(request), cancellationToken);
        var dto = await sender.Send(new GetExpenseTypeByIdQuery(entity.Id), cancellationToken);
        return CreatedAtAction(nameof(GetTypeById), new { id = entity.Id }, dto);
    }

    [HttpPut("types/{id:guid}")]
    public async Task<ActionResult<ExpenseTypeDto>> UpdateType(
        Guid id,
        [FromBody] UpdateExpenseTypeRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateExpenseTypeCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetExpenseTypeByIdQuery(id), cancellationToken);
        return Ok(dto);
    }

    [HttpPost("types/{id:guid}/status")]
    public async Task<ActionResult<ExpenseTypeDto>> SetTypeStatus(
        Guid id,
        [FromBody] SetExpenseTypeStatusRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SetExpenseTypeStatusCommand(id, request), cancellationToken);
        var dto = await sender.Send(new GetExpenseTypeByIdQuery(id), cancellationToken);
        return Ok(dto);
    }
}
