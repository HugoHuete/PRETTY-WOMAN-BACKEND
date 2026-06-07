# Money and profit model

## Conceptos principales

El sistema debe distinguir entre:

```txt
venta registrada
pago recibido
movimiento financiero
ganancia
flujo de caja
```

No son lo mismo.

## Sales

`sales` representa la venta comercial.

Campos sugeridos:

```txt
subtotal_before_discount
total_discount
subtotal_products
shipping_cost
total
gross_profit
payment_commission_total
net_profit_before_expenses
```

## Sale details

`sale_details` debe guardar datos congelados por línea:

```txt
original_sale_price
discount_amount
final_sale_price
cost_at_sale
gross_profit
```

La ganancia por línea puede calcularse como:

```txt
(final_sale_price - cost_at_sale) * quantity
```

## Sale payments

`sale_payments` representa dinero recibido.

Debe soportar:

- pagos parciales
- pagos mixtos
- efectivo
- transferencia
- tarjeta
- POS con comisión

Campos relevantes:

```txt
amount
commission_amount
net_received_amount
payment_date
payment_method_id
payment_terminal_id
```

## Financial movements

`financial_movements` representa flujo real de dinero.

Para pagos de venta, debe referenciar `sale_payment_id`.

Ventaja:

```txt
Venta C$1000
Pago hoy C$500
Pago mañana C$500
```

Solo se registran movimientos financieros cuando entra el dinero.

## Comisiones POS

Si el cliente paga C$500 con una terminal de 4.5%:

```txt
amount = 500
commission_amount = 22.50
net_received_amount = 477.50
```

La comisión debe quedar congelada en el pago.

## Ganancia bruta

Se puede guardar en `sales.gross_profit`.

Debe considerar:

```txt
subtotal de productos después de descuento
- costo de productos vendidos
+ margen de envío
```

Si quieres ganancia después de comisión POS:

```txt
gross_profit - payment_commission_total
```

## Gastos

Los gastos operativos se registran en `financial_movements` con tipo `Expense` y categoría en `expense_categories`.

## Préstamos

Préstamos recibidos no son ganancia.

Pagos de préstamo no son gastos operativos normales.

Deben analizarse separados para no distorsionar utilidad.
