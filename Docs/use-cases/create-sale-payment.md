# Crear pago de venta

## Endpoint

`POST /api/v1/sales/{saleId}/payment-movements`

## Request

```json
{
  "movementDate": "2026-07-11T14:00:00Z",
  "paymentMethodId": 2,
  "productAmount": 500.00,
  "shippingAmount": 90.00,
  "saleDeliveryId": 25
}
```

`grossAmount` is calculated as `productAmount + shippingAmount` and is not accepted in the request. A product-only payment sends `shippingAmount: 0`.

## Flow

1. Load the sale and verify it is not cancelled.
2. Validate the payment method and terminal when needed.
3. Validate the product/shipping allocation and its referenced delivery.
4. Create one payment movement and, for direct payments, one financial movement for its net amount.
5. Recalculate the product payment status and the active delivery amount to collect.

## Examples

A C$590 transfer that pays C$500 of products and C$90 of shipping creates one payment and one financial movement for C$590. The sale is paid, the shipping is paid, and the delivery amount to collect becomes zero.

A C$500 product-only payment is explicit:

```json
{
  "paymentMethodId": 1,
  "productAmount": 500.00,
  "shippingAmount": 0
}
```