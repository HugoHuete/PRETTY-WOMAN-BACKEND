# Create Delivery Agency Reconciliation

## Endpoint

`POST /api/v1/delivery-agency-reconciliations`

## Request

```json
{
  "deliveryAgencyId": 1,
  "reconciliationDate": "2026-07-12T18:00:00Z",
  "settlementExchangeRate": 36.00,
  "comments": "Liquidacion diaria",
  "deliveries": [
    {
      "saleDeliveryId": 25,
      "amountCollectedNio": 0.00,
      "amountCollectedUsd": 20.00,
      "collectionExchangeRate": 36.00,
      "changeGivenNio": 320.00,
      "shippingPaidToAgency": 100.00
    }
  ],
  "postSaleDeliveries": [
    {
      "postSaleDeliveryId": 12,
      "amountCollectedNio": 150.00,
      "amountCollectedUsd": 0.00,
      "changeGivenNio": 0.00
    }
  ]
}
```

In this example, the customer owed C$400. The agency collected $20, applied C$36/USD, and gave C$320 in change. Therefore the net customer collection is C$400.

## Flow

1. Validate the agency and require at least one sale delivery or post-sale delivery.
2. Load the indicated deliveries and require that all belong to the agency, are completed or failed, and have not been reconciled before.
3. Record the customer collection, USD collection rate, NIO change, and shipping cost on each delivery.
4. For completed deliveries, create an agency-sourced payment for the amount actually collected. The payment applies to product debt first and then shipping debt, while preserving `amountCollectedNio`, `amountCollectedUsd`, `changeGivenNio`, and `collectionExchangeRate` as the payment tender.
5. Update the sale payment status without creating a financial movement for that payment.
6. Calculate settlement amounts: collected NIO/USD from sale and post-sale deliveries are remittances received; NIO change plus shipping costs are the amount paid to the agency. Post-sale deliveries must belong to that agency, be in transit or completed, and not have been reconciled before.
7. Create the reconciliation and link every delivery, payment, and financial movement to it.
8. Create an incoming financial movement for the NIO-equivalent remittance received and an outgoing movement for the NIO amount paid to the agency, when their amounts are greater than zero.

## Validation

- `settlementExchangeRate` must be positive.
- All monetary input is non-negative.
- USD collection requires `collectionExchangeRate`; when no USD is collected, that field must be omitted.
- An agency collection may combine NIO and USD in the same delivery. This exception is limited to agency reconciliation payments; direct sale payments still use one currency per movement.
- Customer collection cannot exceed the delivery's `amountToCollect`.
- A post-sale delivery for an exchange must collect exactly its `amountToCollect`; return deliveries cannot collect from the customer.
- Failed deliveries cannot record customer collections, but may have a shipping cost.
