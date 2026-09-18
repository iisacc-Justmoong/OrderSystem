using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Data;
using OrderSystem.DTOs;
using OrderSystem.Models;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(AppDbContext dbContext) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerResponse>> Get(
        long id,
        CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => customer.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);

        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Customers
            .AnyAsync(customer => customer.Email == email, cancellationToken);

        if (emailExists)
        {
            return Conflict("A customer with the same email already exists.");
        }

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = NormalizeOptionalText(request.Phone),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = customer.ToResponse();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, response);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
