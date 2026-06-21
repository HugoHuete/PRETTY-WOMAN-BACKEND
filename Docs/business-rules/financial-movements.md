# Financial Movements Business Rules

## Objetivo

Registrar movimientos reales de dinero del negocio, separando ventas, pagos, gastos, préstamos, inversiones y retiros.

## Tablas principales

- `financial_movements`
- `financial_movement_types`
- `financial_movement_directions`
- `expense_categories`
- `sale_payments`
- `orders`
- `loans`

## Regla: un movimiento financiero representa dinero real

`financial_movements` debe representar entradas o salidas reales de dinero.

No debe representar simplemente que existe una venta.

Ejemplo:

- Venta registrada pero no pagada: no hay movimiento financiero todavía.
- Pago recibido: sí hay movimiento financiero.

## Regla: referencia a `sale_payments`

Los ingresos por ventas deben relacionarse con `sale_payments`, no directamente con `sales`.

Razón:

- Una venta puede tener varios pagos.
- Los pagos pueden ocurrir en fechas distintas.
- Cada pago puede tener método diferente.
- Cada pago puede tener comisión diferente.

## Regla: direcciones financieras

`financial_movement_directions` debe indicar si el movimiento es:

- `In`
- `Out`

Ejemplos:

- Venta pagada: `In`
- Gasto de empaque: `Out`
- Préstamo recibido: `In`
- Pago de préstamo: `Out`
- Inversión del dueño: `In`
- Retiro del dueño: `Out`

## Regla: tipos financieros

Tipos sugeridos para `financial_movement_types`:

- `SalePayment`
- `Expense`
- `SupplierPayment`
- `LoanReceived`
- `LoanPayment`
- `OwnerInvestment`
- `OwnerWithdrawal`
- `Adjustment`
- `CardCommission`

## Regla: gastos deben tener categoría

Cuando el tipo sea `Expense`, se debe indicar `expense_category_id`.

Categorías sugeridas:

- `Packaging`
- `Advertising`
- `Transport`
- `Rent`
- `Utilities`
- `Payroll`
- `Maintenance`
- `OfficeSupplies`
- `Other`

## Regla: comisiones de POS

La comisión POS puede manejarse de dos maneras:

1. Guardada en `sale_payments.commission_amount` para reportes rápidos.
2. Opcionalmente crear movimiento financiero tipo `CardCommission` como egreso.

Si se crea movimiento financiero por comisión, debe evitarse restarla doble en reportes.

## Regla: pagos a proveedor

En el flujo actual de compras, crear una orden representa el pago al proveedor y debe registrar un egreso en `financial_movements`.

El movimiento debe relacionarse con `order_id`, tener tipo `SupplierPayment`, dirección `Out` y monto igual a `orders.total_cost_nio`.

Si en el futuro se permiten órdenes no pagadas o pagos parciales, esta regla debe cambiar para registrar el movimiento financiero en el momento real del pago, no al crear la orden.

## Regla: préstamos

Un préstamo recibido debe registrar:

- registro en `loans`
- movimiento financiero de ingreso tipo `LoanReceived`

Un pago a préstamo debe registrar:

- reducción o actualización del préstamo según la lógica definida
- movimiento financiero de egreso tipo `LoanPayment`

## Regla: utilidad vs flujo de caja

No confundir utilidad con flujo de caja.

Utilidad:

```txt
ventas - costo de productos - descuentos - comisiones - gastos
```

Flujo de caja:

```txt
dinero entrante real - dinero saliente real
```

`financial_movements` sirve principalmente para flujo de caja.

`sale_details`, `sales` y costos históricos sirven para utilidad.
