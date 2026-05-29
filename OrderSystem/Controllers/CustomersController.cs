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
    public async Task<ActionResult<CustomerResponse>> Get(long id, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FindAsync([id], cancellationToken);
        return customer is null
            ? NotFound()
            : Ok(customer.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = customer.ToResponse();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, response);
    }
}
