# Cancelar venta

## Objetivo

Cancelar una venta completa antes de completarla, sin borrar su historial. Las devoluciones posteriores se registran mediante `SaleReturn` y los cambios mediante `SaleExchange`.

## Cuándo aplica

- La venta fue registrada por error o la clienta canceló antes de recibir.
- La venta no está completada.
- No existe un envío enviado a la agencia ni pendiente de retorno por selección.
- Los pagos ya fueron reembolsados por medio de sus movimientos.

## Tablas involucradas

- `sales`
- `sale_products`
- `sale_deliveries`
- `sale_payment_movements`
- `products`
- `inventory_movements`
- `product_holds`

## Flujo esperado

1. Buscar la venta y validar que no esté cancelada ni completada.
2. Rechazar la cancelación si algún envío está `Sent` o `DeliveredPendingSelection`.
3. Confirmar que el saldo neto de `sale_payment_movements` sea cero; de lo contrario, primero se debe registrar el reembolso.
4. Cancelar los envíos que estén `Pending`.
5. Si la venta ya afectó el inventario, revertir cada línea vendida:
   - aumentar `available_quantity`, si ya se había descontado
   - crear un `inventory_movement` tipo `SaleCancelled`, vinculado a la línea original
6. Liberar los `product_holds` activos: mover la cantidad de `unavailable_quantity` a `available_quantity`, marcar el hold como `NotSelected` y registrar `SelectionReturned`.
7. Cambiar `sales.sale_status_id` a `Cancelled`.

## Reglas de negocio

- No se debe borrar la venta.
- Las líneas de venta conservan precios, costos e importes históricos; no tienen un estado propio.
- Si hubo pago, debe reembolsarse antes de cancelar la venta.
- Los envíos pendientes se cancelan; los ya enviados bloquean la cancelación.
- Las devoluciones y cambios posteriores se modelan en sus propios agregados, no mediante estados en la línea de venta.

## Errores esperados

- Venta inexistente.
- Venta ya cancelada.
- Venta completada.
- Envío enviado a la agencia o pendiente de retorno por selección.
- Pagos pendientes de reembolso.
