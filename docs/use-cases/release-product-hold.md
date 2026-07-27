# Liberar product hold

## Objetivo

Cerrar un hold de seleccion cuando el producto no fue escogido y regresa a inventario disponible.

## Tablas involucradas

- `product_holds`
- `products`
- `inventory_movements`

## Flujo esperado

1. Buscar el `product_hold`.
2. Validar que este `Active`.
3. Cambiar estado a `NotSelected`.
4. Colocar `resolved_at`.
5. Disminuir `products.unavailable_quantity`.
6. Aumentar `products.available_quantity`.
7. Crear `inventory_movements` relacionado con `product_hold_id`.

## Reglas

- No eliminar el hold.
- No usar este flujo para reservas con pago.
- No usar este flujo para productos danados, sucios, perdidos o en revision.
