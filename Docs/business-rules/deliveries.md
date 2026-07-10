# Delivery Business Rules

## Scope

A delivery belongs to a sale and records one delivery attempt. A sale can have historical deliveries, but only one may be active at a time.

## Active delivery

A delivery is active while its status is neither `Completed` nor `Cancelled`.

- A sale cannot create a second active delivery.
- A new delivery is allowed only after every previous delivery is `Completed` or `Cancelled`.
- Completed and cancelled sales cannot create or send a delivery.
- A local sale (`InStoreSale`) cannot have a delivery.
- A sale cannot be changed to `InStoreSale` while it has an active delivery.

## Sale states

- Creating a delivery sets the sale to `ReadyForDelivery`.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/send` sets the sale to `SentForDelivery`.
- The delivery remains `Pending` until a later operational process marks it `Completed` or `Cancelled`.
- A completed or cancelled sale must not retain an active delivery. The current API does not expose those sale transitions; any future endpoint for them must first require that all deliveries are terminal.

## Collection amount

For an agency with `can_collect_cash_on_delivery = true`:

`amount_to_collect = max(0, sale.total - net_payment_total) + shipping_charged_to_client`

`net_payment_total` includes payments and refunds. The amount is recalculated when payments, refunds, grouped payment adjustments, or sale products change while the delivery is active.

For an agency with `can_collect_cash_on_delivery = false`:

- The sale product total must be fully paid; an overpayment also satisfies this rule.
- `shipping_charged_to_client` remains the price charged to the customer for the delivery.
- `amount_to_collect` is zero because the agency cannot collect on delivery.

The amount collected by the agency is not a sale payment until the business receives the remittance.

## Creation input

Creating a delivery accepts only its operational data: code, municipality, agency, optional client, shipping charged to the client, and comments. `shipping_charged_to_client` is not a separate cash-on-delivery instruction; the agency only follows `amount_to_collect`.

`amount_collected_nio`, `amount_collected_usd`, and `shipping_paid_to_agency` are not received here. They belong to the later agency reconciliation process.