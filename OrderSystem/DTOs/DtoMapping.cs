using OrderSystem.Models;

namespace OrderSystem.DTOs;

public static class DtoMapping
{
    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price,
            product.IsActive,
            product.Inventory?.Quantity ?? 0,
            product.CreatedAt,
            product.UpdatedAt);
    }

    public static CustomerResponse ToResponse(this Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.CreatedAt);
    }

    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Customer?.Name,
            order.Status,
            order.TotalAmount,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items
                .OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse(
                    item.ProductId,
                    item.Product?.Name,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal))
                .ToList());
    }

    public static PaymentResponse ToResponse(this Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.PaymentMethod,
            payment.Status,
            payment.Amount,
            payment.PaidAt,
            payment.CreatedAt);
    }
}
