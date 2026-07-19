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

- Creating a delivery sets the sale to `ReadyForDelivery`. If the sale was `Pending`, its products move from `Available` to `Reserved`; an already reserved sale is not reserved twice.
- `PATCH /api/v1/sales/{saleId}/deliveries/{deliveryId}` may update the client, historical address, agency, municipality, code, and shipping charge while the delivery is pending.
- The update recalculates `amount_to_collect` and applies the selected agency collection rule.
- A delivery can be edited only while it is `Pending`.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/send` transitions a delivery from `Pending` to `Sent`, moves the reserved quantities to `OutOfInventory`, and sets the sale to `SentForDelivery`.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/complete` transitions a sent delivery to `Completed` only when los productos y el envio ya estan pagados completamente.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/fail` transitions a sent delivery to `Failed`, returns only the quantities still outside to `Reserved`, and returns the sale to `ReadyForDelivery` for a new attempt. The transition is rejected while a return is requested or picked up but not received, or while an exchange remains requested before its physical handover.
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/cancel` transitions a pending delivery to `Cancelled` and returns the sale to `ReadyForDelivery`.
- A completed or cancelled sale must not retain an active delivery.

## Collection amount

For an agency with `can_collect_cash_on_delivery = true`:

```text
amount_to_collect =
  max(0, sale.total - paid_products)
  + max(0, shipping_charged_to_client - paid_shipping_for_delivery)
```

`paid_products` is the net total of payments and refunds allocated to products. `paid_shipping_for_delivery` is the net total allocated to this specific delivery's shipping. Payments for shipping never reduce the product balance, and payments for products never reduce the shipping balance.

The amount is recalculated when payments, refunds, grouped payment adjustments, the sale products, or the delivery shipping charge change while the delivery is active. The API response's `amountToCollect` is the authoritative cash-on-delivery instruction; clients must display that value rather than recalculate it locally.

For an agency with `can_collect_cash_on_delivery = false`:

- The sale product total must be fully paid; an overpayment also satisfies this rule.
- `shipping_charged_to_client` remains the price charged to the customer for the delivery.
- `amount_to_collect` is zero because the agency cannot collect on delivery.

The amount collected by the agency is not a sale payment until its delivery is included in an agency reconciliation.

For cash-on-delivery deliveries, a `Sent` delivery is reconciled only when the agency reports the full outstanding amount. The reconciliation records that payment and completes both the delivery and the sale. If the client cannot pay the full amount, the agency must not deliver the products: the delivery is marked `Failed` and no partial collection is recorded.

## Roles operativos

- Vendedor (`Employee`) puede crear un envio, lo que deja la venta en `ReadyForDelivery`, y marcar un envio pendiente como `Sent`.
- Admin gestiona correcciones, cancelaciones, entregas fallidas, completadas, entregadas con seleccion pendiente y conciliaciones.

## Agency reconciliation

A `delivery_agency_reconciliation` groups completed or failed deliveries from exactly one agency. Each delivery can belong to only one reconciliation.

During reconciliation, the delivery records the cash received from the customer, the shipping cost charged by the agency, and, when applicable, the NIO change given for an USD payment.

```text
net_customer_collection =
  amount_collected_nio
  + (amount_collected_usd * collection_exchange_rate)
  - change_given_nio
```

- Completed deliveries can collect from zero up to `amount_to_collect`.
- Failed deliveries cannot record a customer collection, but can record `shipping_paid_to_agency`.
- USD collections require `collection_exchange_rate`, the actual rate applied by the agency for that delivery.
- The reconciliation stores its own `settlement_exchange_rate` to value the USD remittance received. It is historical input, not a rate inferred from the rate catalog.
- A collection payment is created for the amount actually collected. It settles products first and then the delivery charge, and does not create a direct financial movement.

The reconciliation calculates its financial amounts from the delivery details. It creates up to two financial movements of type `DeliveryAgencyReconciliation`:

- An `In` movement for the NIO-equivalent value received from the agency.
- An `Out` movement for the NIO amount paid to the agency, including change reimbursements and shipping costs.

These are cash-flow movements, not operational expenses. Shipping margin remains `shipping_charged_to_client - shipping_paid_to_agency`.

## Creation input

Creating a delivery accepts only its operational data: code, municipality, agency, optional client, historical delivery address, shipping charged to the client, and comments. `shipping_charged_to_client` is not a separate cash-on-delivery instruction; the agency only follows `amount_to_collect`.

`delivery_address` is copied from the request or, when omitted, from the associated client address. It is historical and must not change if the client updates their profile later.

`amount_collected_nio`, `amount_collected_usd`, and `shipping_paid_to_agency` are not received here. They belong to the later agency reconciliation process.
