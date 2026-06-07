# Liberar reserva de producto

## Objetivo

Regresar al inventario disponible un producto previamente reservado.

## Cuándo aplica

- La clienta no escogió esa talla.
- La clienta canceló.
- El producto regresó a tienda.
- La reserva expiró.
- Se apartó por error.

## Tablas involucradas

- `product_holds`
- `products`
- `inventory_movements`
- `inventory_movement_types`

## Flujo esperado

1. Buscar `product_hold`.
2. Validar que esté en estado `Active`.
3. Cambiar estado a `Released`.
4. Guardar `resolved_at`.
5. Disminuir `products.reserved_quantity`.
6. Aumentar `products.available_quantity`.
7. Crear `inventory_movements` tipo `ReservationReleased`.

## Reglas de negocio

- No se puede liberar una reserva ya liberada.
- No se puede liberar una reserva convertida a venta.
- No se puede liberar más cantidad de la reservada.
- La liberación debe dejar rastro en inventario.

## Errores esperados

- Reserva inexistente.
- Reserva no activa.
- Cantidades inconsistentes.
