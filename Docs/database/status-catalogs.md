# Status catalogs

Este documento lista estados sugeridos para catálogos del sistema.

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
Confirmed
PartiallyPaid
Paid
Delivered
Cancelled
```

La venta puede estar pagada pero no entregada, o entregada con varios envíos históricos.

## sale_detail_statuses

```txt
Active
Cancelled
Refunded
Exchanged
```

Una venta completa puede seguir activa aunque una línea se haya cambiado o reembolsado.

## delivery_statuses

```txt
Pending
Sent
Delivered
Failed
Returned
Rescheduled
Cancelled
```

Cada envío conserva su propio estado.

## product_hold_statuses

Si decides usar tabla catálogo en vez de varchar:

```txt
Active
Released
ConvertedToSale
Cancelled
Expired
```

## inventory_movement_types

```txt
PurchaseReceived
Sale
SaleCancelled
Return
ExchangeReturn
ExchangeSale
Damaged
Lost
Adjustment
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
