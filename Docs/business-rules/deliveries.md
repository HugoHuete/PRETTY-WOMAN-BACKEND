# Delivery Business Rules

## Scope

A delivery belongs to a sale and records one delivery attempt. A sale can have historical deliveries, but only one may be active at a time.

## Active delivery

A delivery is active while its status is neither `Completed`, `Cancelled`, nor `Failed`.

- A sale cannot create a second active delivery.
- A new delivery is allowed only after every previous delivery is `Completed`, `Cancelled`, or `Failed`.
- Completed and cancelled sales cannot create or send a delivery.
- A local sale (`InStoreSale`) cannot have a delivery.
- A sale cannot be changed to `InStoreSale` while it has an active delivery.

## Sale states

- Creating a delivery sets the sale to `ReadyForDelivery`.
- `PATCH /api/v1/sales/{saleId}/deliveries/{deliveryId}` may update the client, historical address, agency, municipality, code, and shipping charge while the delivery is pending.
- The update recalculates `amount_to_collect` and applies the selected agency collection rule.
- A delivery can be edited only while it is `Pending`.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/send` transitions a delivery from `Pending` to `Sent` and sets the sale to `SentForDelivery`.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/complete` transitions a sent delivery to `Completed` and completes the sale.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/fail` transitions a sent delivery to `Failed` and returns the sale to `ReadyForDelivery` for a new attempt.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/cancel` transitions a pending delivery to `Cancelled` and returns the sale to `ReadyForDelivery`.
- A completed or cancelled sale must not retain an active delivery.

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

Creating a delivery accepts only its operational data: code, municipality, agency, optional client, historical delivery address, shipping charged to the client, and comments. `shipping_charged_to_client` is not a separate cash-on-delivery instruction; the agency only follows `amount_to_collect`.

`delivery_address` is copied from the request or, when omitted, from the associated client address. It is historical and must not change if the client updates their profile later.

`amount_collected_nio`, `amount_collected_usd`, and `shipping_paid_to_agency` are not received here. They belong to the later agency reconciliation process.