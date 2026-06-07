# Payments Business Rules

## Objetivo

Registrar pagos reales recibidos por ventas, permitiendo pagos parciales, pagos mixtos y comisiones de POS.

## Tablas principales

- `sale_payments`
- `payment_methods`
- `payment_terminals`
- `sales`
- `financial_movements`

## Regla: una venta puede tener varios pagos

No se debe asumir que una venta tiene un único método de pago.

Ejemplo:

- Venta total: C$1,000.
- C$500 efectivo.
- C$500 tarjeta.

Esto debe generar dos registros en `sale_payments`.

## Regla: `sale_payments` representa dinero recibido

Cada registro en `sale_payments` representa un pago real recibido o registrado.

Debe guardar:

- venta asociada
- fecha del pago
- método de pago
- terminal de pago si aplica
- monto cobrado
- comisión
- neto recibido
- usuario que registró el pago

## Regla: pagos con tarjeta y POS

Cuando el método de pago sea tarjeta, debe indicarse el POS/terminal utilizado si aplica.

Cada terminal puede tener una comisión distinta.

Ejemplo:

- POS A: 4.5%
- POS B: 5.5%

El pago debe guardar la comisión y el neto recibido al momento del pago.

## Regla: congelar comisión histórica

Aunque el porcentaje del POS cambie en el futuro, los pagos históricos no deben recalcularse.

Por eso se guardan:

- `commission_amount`
- `net_received_amount`

Recomendación adicional: guardar también el porcentaje aplicado al momento del pago si se quiere trazabilidad completa.

## Regla: cálculo de neto recibido

Para efectivo o transferencia sin comisión:

```txt
net_received_amount = amount
commission_amount = 0
```

Para tarjeta:

```txt
commission_amount = amount * commission_percentage / 100
net_received_amount = amount - commission_amount
```

## Regla: movimiento financiero desde pagos

El movimiento financiero relacionado a una venta debe originarse desde `sale_payments`, no desde `sales`.

Razón:

- La venta representa la transacción comercial.
- El pago representa el ingreso real de dinero.

Si una venta se paga en dos fechas, deben existir dos movimientos financieros, cada uno relacionado al pago correspondiente.

## Regla: estado de pago de venta

El estado de la venta puede derivarse de la suma de pagos:

- Si suma de pagos es 0: pendiente.
- Si suma de pagos es menor que total: parcialmente pagada.
- Si suma de pagos cubre total: pagada.

Se puede guardar el estado para facilitar consultas, pero debe actualizarse de forma controlada desde el servicio de ventas/pagos.
