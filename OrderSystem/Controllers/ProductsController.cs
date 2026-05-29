using Microsoft.AspNetCore.Mvc;
using OrderSystem.DTOs;
using OrderSystem.Services;

namespace OrderSystem.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ProductService productService) : ControllerBase
{
    private readonly ProductService _productService = productService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(CancellationToken cancellationToken)
    {
        var products = await _productService.ListAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductResponse>> Get(long id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetAsync(id, cancellationToken);
        return product is null
            ? NotFound()
            : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
        }
        catch (DomainException exception)
        {
            return ToProblem(exception);
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProductResponse>> Update(
        long id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productService.UpdateAsync(id, request, cancellationToken);
            return Ok(response);
        }
        catch (DomainException exception)
        {
            return ToProblem(exception);
        }
    }

    private ObjectResult ToProblem(DomainException exception)
    {
        return Problem(statusCode: exception.StatusCode, title: exception.Message);
    }
}
