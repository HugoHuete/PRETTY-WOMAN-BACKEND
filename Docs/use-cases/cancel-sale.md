# Cancelar venta

## Objetivo

Cancelar una venta completa manteniendo historial.

## Cuándo aplica

- Clienta cancela antes de recibir.
- Venta registrada por error.
- No hubo pago.
- Se canceló todo el pedido.

## Tablas involucradas

- `sales`
- `sale_statuses`
- `sale_details`
- `sale_payments`
- `products`
- `inventory_movements`
- `financial_movements`
- `product_holds`

## Flujo esperado

1. Buscar venta.
2. Validar que no esté cancelada.
3. Cambiar estado de venta a `Cancelled`.
4. Cambiar estado de líneas activas a `Cancelled`.
5. Revertir inventario de líneas vendidas:
   - aumentar `available_quantity`, si ya se había descontado
   - crear movimiento tipo `SaleCancelled`
6. Liberar reservas activas asociadas.
7. Si hubo pagos, registrar reembolso o movimiento financiero correspondiente.
8. Registrar comentario o motivo de cancelación.

## Reglas de negocio

- No se debe borrar la venta.
- La cancelación debe conservar historial.
- Si hubo pago, debe existir movimiento financiero de salida o ajuste.
- Si hubo envío, no borrar `sale_deliveries`; cambiar estado si corresponde.

## Errores esperados

- Venta inexistente.
- Venta ya cancelada.
- Inventario inconsistente.
- Reembolso mayor que lo pagado.
