# Cambiar producto vendido

## Objetivo

Registrar el cambio de un producto vendido por otra talla u otro producto.

## Cuándo aplica

- Cambio por otra talla.
- Cambio por otro color.
- Cambio por otro producto.
- Cambio con pago de diferencia.
- Cambio con devolución parcial.

## Tablas involucradas

- `sales`
- `sale_exchanges`
- `exchange_return_items`
- `exchange_outbound_items`
- `products`
- `inventory_movements`
- `financial_movements`
- `sale_payments`

## Flujo esperado

1. Buscar venta y línea original.
2. Crear un `SaleExchange` con sus ítems de retorno y salida.
3. Reservar las prendas de salida.
4. Al completar la entrega, registrar el retorno original como pendiente de llegada física y entregar las prendas de reemplazo.
5. Al recibir el retorno en tienda, actualizar el estado del ítem de retorno y registrar su movimiento de inventario.

## Reglas de negocio

- No crear una venta nueva si el cambio pertenece a la misma transacción comercial.
- La venta y sus líneas originales conservan todo el historial y no se modifican.
- El estado del cambio se mantiene en `SaleExchange` y sus ítems, no en `SaleProduct`.
- Si el producto original vuelve dañado, no debe regresar a stock disponible.

## Errores esperados

- Línea original no activa.
- Stock insuficiente del nuevo producto.
- Diferencia de pago inválida.
- Venta cancelada.
