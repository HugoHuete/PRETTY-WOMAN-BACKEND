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

## Pagos en dólares

El monto aplicado a productos y envío siempre se expresa en córdobas. Para pagos en dólares por efectivo o transferencia, el request agrega `amountReceivedUsd` y `exchangeRate`; opcionalmente `changeGivenNio` registra el cambio entregado en córdobas cuando corresponda. Un movimiento recibe una sola moneda: para pagos mixtos se crean movimientos separados. Los pagos con tarjeta se registran únicamente en córdobas.

```json
{
  "paymentMethodId": 1,
  "productAmount": 800.00,
  "amountReceivedUsd": 21.85,
  "exchangeRate": 36.62
}
```

En este ejemplo, la venta queda pagada por C$800.00. El sistema conserva el monto recibido ($21.85), la tasa usada y `exchangeDifferenceNio` (C$0.15) sin convertir esa diferencia en saldo pendiente ni reembolso. El movimiento financiero registra C$800.15 para que el balance represente el monto convertido realmente recibido. Antes del cálculo, el backend normaliza los montos a 2 decimales y la tasa a 4, que son las escalas persistidas; primero redondea el total convertido y después deriva la diferencia contra el monto aplicado. Los pagos en USD no se editan; para corregirlos se reembolsa el pago completo y se crea uno nuevo.

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
