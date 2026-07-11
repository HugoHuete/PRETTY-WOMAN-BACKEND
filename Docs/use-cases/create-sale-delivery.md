# Create Sale Delivery

## Endpoints

- `POST /api/v1/sales/{saleId}/deliveries`
- `PATCH /api/v1/sales/{saleId}/deliveries/{deliveryId}`
- `POST /api/v1/sales/{saleId}/deliveries/{deliveryId}/send`

## Create request

```json
{
  "code": "DEL-001",
  "municipalityId": 1,
  "deliveryAgencyId": 2,
  "clientId": 15,
  "shippingChargedToClient": 60.00,
  "deliveryAddress": "De la rotonda 2 cuadras al norte",
  "comments": "Llamar antes de llegar"
}
```

`clientId` is optional. When omitted, the sale client is used when available. `deliveryAddress` is optional; when omitted, the current address of the resolved client is copied into the delivery as historical data.

## Create flow

1. Load the sale and validate that it is neither local, completed, nor cancelled.
2. Verify that no active delivery exists for the sale.
3. Validate the municipality and enabled delivery agency.
4. Calculate the net amount already paid by the customer.
5. Validate the agency collection capability and calculate `amount_to_collect`.
6. Create a `Pending` delivery.
7. Move the sale to `ReadyForDelivery`.

## Send flow

The send endpoint verifies that the delivery belongs to the sale and remains active. It then changes the sale to `SentForDelivery`.

## Collection semantics

`shippingChargedToClient` is the delivery price charged to the customer, whether it was paid before dispatch or is included in the amount to collect. The delivery agency only uses `amountToCollect` as its cash-on-delivery instruction.

## Deferred fields

The creation request does not accept `amountCollectedNio`, `amountCollectedUsd`, or `shippingPaidToAgency`. Those fields are recorded during a later reconciliation with the agency.
## Update request

`PATCH /api/v1/sales/{saleId}/deliveries/{deliveryId}` accepts any combination of these fields:

```json
{
  "clientId": 16,
  "deliveryAddress": "Nueva direccion",
  "deliveryAgencyId": 3,
  "municipalityId": 2,
  "code": "DEL-001-A",
  "shippingChargedToClient": 75.00
}
```

The endpoint validates the updated municipality, client, and enabled agency. It recalculates `amountToCollect`, including the rule that a non-COD agency requires the sale to be fully paid. A delivery cannot be updated after it has been sent to the agency, completed, or cancelled.