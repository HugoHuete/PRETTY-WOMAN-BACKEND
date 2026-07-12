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
  ]
}
```

In this example, the customer owed C$400. The agency collected $20, applied C$36/USD, and gave C$320 in change. Therefore the net customer collection is C$400.

## Flow

1. Validate the agency and require at least one delivery.
2. Load the indicated deliveries and require that all belong to the agency, are completed or failed, and have not been reconciled before.
3. Record the customer collection, USD collection rate, NIO change, and shipping cost on each delivery.
4. For completed deliveries, create an agency-sourced cash payment for the amount actually collected. The payment applies to product debt first and then shipping debt.
5. Update the sale payment status without creating a financial movement for that payment.
6. Calculate the settlement amounts from the delivery details: collected NIO/USD are remittances received; NIO change plus shipping paid are the amount paid to the agency.
7. Create the reconciliation and link every delivery, payment, and financial movement to it.
8. Create an incoming financial movement for the NIO-equivalent remittance received and an outgoing movement for the NIO amount paid to the agency, when their amounts are greater than zero.

## Validation

- `settlementExchangeRate` must be positive.
- All monetary input is non-negative.
- USD collection requires `collectionExchangeRate`; when no USD is collected, that field must be omitted.
- Customer collection cannot exceed the delivery's `amountToCollect`.
- Failed deliveries cannot record customer collections, but may have a shipping cost.
