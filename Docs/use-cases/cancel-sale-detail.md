# Cancelar producto de una venta

## Objetivo

Cancelar o reembolsar una línea específica de una venta sin cancelar toda la venta.

## Cuándo aplica

- La clienta devuelve una prenda.
- Una prenda no estaba disponible.
- Se reembolsa solo un producto.
- Se cancela una línea antes de enviar.

## Tablas involucradas

- `sale_details`
- `sale_detail_statuses`
- `sales`
- `products`
- `inventory_movements`
- `financial_movements`
- `sale_payments`

## Flujo esperado

1. Buscar línea de venta.
2. Validar que esté activa.
3. Cambiar estado a `Cancelled` o `Refunded`.
4. Devolver inventario si el producto debe regresar a stock.
5. Crear `inventory_movements` tipo `SaleProductCancelled` o `Return`.
6. Si corresponde, crear movimiento financiero de reembolso.
7. Recalcular totales de la venta si el sistema lo requiere.
8. Recalcular ganancia de la venta.

## Reglas de negocio

- Una línea cancelada no debe contarse como producto vendido activo.
- Una línea reembolsada debe conservar precio histórico.
- El reembolso financiero debe estar separado del cambio de estado de la línea.
- Si el producto vuelve dañado, no debe regresar a `available_quantity`; debe registrarse como dañado.

## Errores esperados

- Línea inexistente.
- Línea ya cancelada.
- Venta cancelada.
- Reembolso inválido.
