# Crear reserva de producto

## Objetivo

Apartar temporalmente un producto para que deje de aparecer como disponible, sin registrarlo todavía como vendido.

## Cuándo aplica

- Se envían dos tallas para que la clienta escoja una.
- Se aparta un producto mientras la clienta confirma pago.
- Se retira un producto temporalmente de tienda.
- Se reserva un producto para una venta pendiente.

## Tablas involucradas

- `products`
- `product_holds`
- `inventory_movements`
- `inventory_movement_types`
- `sales`, opcional
- `sale_deliveries`, opcional

## Flujo esperado

1. Buscar producto.
2. Validar que `available_quantity >= quantity`.
3. Crear `product_hold` con estado `Active`.
4. Disminuir `products.available_quantity`.
5. Aumentar `products.reserved_quantity`.
6. Crear `inventory_movements` tipo `ReservationCreated`.
7. Asociar la reserva a `sale_id` o `sale_delivery_id` si aplica.

## Reglas de negocio

- Una reserva no es venta.
- Una reserva activa debe reducir la cantidad disponible.
- Toda reserva debe tener una razón.
- Toda reserva debe crear movimiento de inventario.
- No se puede reservar más cantidad que la disponible.

## Ejemplo

Antes:

```txt
available_quantity = 5
reserved_quantity = 0
```

Reserva de 1 unidad:

```txt
available_quantity = 4
reserved_quantity = 1
```

## Errores esperados

- Producto inexistente.
- Stock insuficiente.
- Cantidad inválida.
- Venta inexistente, si se envía `sale_id`.
