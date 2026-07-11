# Payments Business Rules

## Scope

`SalePaymentMovement` records a customer payment and separates what it pays for from the money actually received.

- `product_amount` applies to the products in the sale.
- `shipping_amount` applies to one `sale_delivery`.
- `gross_amount` is the sum of both amounts and is the base for card commissions and the direct financial movement.

## Allocation rules

```text
gross_amount = product_amount + shipping_amount
```

- Amounts cannot be negative and their sum must be greater than zero.
- A payment with `shipping_amount > 0` must reference `sale_delivery_id`.
- A payment with no shipping allocation must not reference a delivery.
- The referenced delivery must belong to the same sale.
- Payments applied to shipping cannot exceed `shipping_charged_to_client` for that delivery.
- In-store sales cannot have a shipping allocation.

Every create request must explicitly provide the amount applied to products and/or shipping. `gross_amount` is calculated and persisted by the service; it is not a request field.

## Sale payment status

Only `product_amount` is used to calculate `sales.sale_payment_status_id`.

- Zero: `Unpaid`.
- Less than `sales.total`: `PartiallyPaid`.
- Equal to `sales.total`: `Paid`.
- Greater after a product change: `RefundPending`.

A shipping payment does not make a product sale look overpaid.

## Delivery collection

For a COD agency:

```text
amount_to_collect =
  max(0, sale.total - paid_products)
  + max(0, shipping_charged_to_client - paid_shipping_for_delivery)
```

For an agency that cannot collect cash on delivery, products must be paid before creating the delivery. Products and shipping must both be paid before it can be sent to the agency.

## Direct payments and financial movements

A direct cash, transfer, or card payment creates one financial movement for its `net_received_amount`. The financial movement represents the entire payment; the product/shipping columns explain its business allocation.

Payments collected by an agency will be handled during delivery reconciliation. They will settle the sale without creating their own direct financial movement; the reconciliation will record the actual net remittance.

## Refunds

Refunds preserve the product and shipping allocation of the refunded amount. A partial refund of a payment that includes shipping must state the amount refunded for each component. Card payments remain full reversals only.