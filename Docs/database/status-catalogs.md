# Status catalogs

Este documento lista estados sugeridos para catalogos del sistema.

## order_statuses

```txt
Pending
Ordered
PartiallyReceived
Received
Cancelled
```

## sale_statuses

```txt
Pending
Reserved
ReadyForDelivery
SentForDelivery
Completed
Cancelled
```

Estos estados describen la etapa operativa de la venta. El pago se calcula aparte desde `sale_payments`.


## sale_payment_statuses

```txt
Unpaid
PartiallyPaid
Paid
```

Estos estados describen cuanto se ha pagado de una venta y se guardan en `sales.sale_payment_status_id`.

## delivery_statuses

```txt
Pending
Sent
Completed
Failed
Cancelled
```

Cada envio conserva su propio estado.

## product_hold_statuses

```txt
Active
ConvertedToSale
NotSelected
```

Estos estados son solo para reservas comerciales de clientas.

## product_inventory_issue_types

```txt
Damaged
Dirty
Missing
UnderReview
Repairing
```

## product_inventory_issue_statuses

```txt
Open
ResolvedToAvailable
Discarded
ConfirmedLost
Cancelled
```

## inventory_stock_buckets

```txt
External
Available
Reserved
Unavailable
OutOfInventory
```

`External` y `OutOfInventory` no son columnas en `products`; sirven para auditar entradas y salidas del inventario activo.

## inventory_movement_types

```txt
PurchaseReceived
Sale
SaleCancelled
CustomerReturn
ExchangeReturn
Damaged
Repaired
Lost
Found
Discarded
Donation
AdjustmentIncrease
AdjustmentDecrease
ReservationCreated
ReservationReleased
ReservationConvertedToSale
```

## financial_movement_types

```txt
SalePayment
Expense
LoanReceived
LoanPayment
OwnerInvestment
OwnerWithdrawal
SupplierPayment
Adjustment
```

## financial_movement_directions

```txt
Income
Expense
```

## discount_types

```txt
Percentage
FixedAmount
FixedPrice
```

## discount_sources

```txt
None
Campaign
Manual
```

## payment_methods

```txt
Cash
Transfer
Card
```
