# Product Holds Business Rules

## Objetivo

Apartar temporalmente productos para una clienta sin registrarlos todavia como vendidos.

Este modulo cubre reservas comerciales: seleccion, prueba, confirmacion o pago pendiente.

No cubre productos danados, sucios, perdidos o en revision. Esos casos se manejan con `product_inventory_issues`.

## Tablas principales

- `product_holds`
- `product_hold_statuses`
- `products`
- `inventory_movements`
- `sales`
- `sale_deliveries`

## Responsabilidad de `product_holds`

`product_holds` representa el estado actual e historico de una reserva comercial.

Sirve para responder:

- Que productos estan apartados para una clienta?
- Para que venta estan apartados?
- Por que estan apartados?
- Cuando se liberaron o convirtieron en venta?

## Estados

- `Active`
- `NotSelected`
- `ConvertedToSale`

## Razones

- `SentForSelection`
- `PendingPayment`
- `ManualHold`
- `ReservedForClient`

## Regla: crear reserva

Cuando se aparta producto:

1. Validar que `available_quantity >= quantity`.
2. Crear `product_hold` con estado `Active`.
3. Disminuir `products.available_quantity`.
4. Aumentar `products.reserved_quantity`.
5. Crear `inventory_movement` tipo `ReservationCreated`.
6. Relacionar el movimiento con `product_hold_id`.

## Regla: liberar reserva

Cuando el producto regresa a tienda o ya no se necesita reservar:

1. Validar que la reserva este `Active`.
2. Cambiar `product_hold.status` a `NotSelected`.
3. Colocar `resolved_at`.
4. Disminuir `products.reserved_quantity`.
5. Aumentar `products.available_quantity`.
6. Crear `inventory_movement` tipo `ReservationReleased`.
7. Relacionar el movimiento con `product_hold_id`.

## Regla: convertir reserva en venta

Cuando la clienta escoge o confirma el producto reservado:

1. Validar que la reserva este `Active`.
2. Crear o confirmar `sale_product`.
3. Cambiar `product_hold.status` a `ConvertedToSale`.
4. Colocar `resolved_at`.
5. Disminuir `products.reserved_quantity`.
6. Crear `inventory_movement` tipo `ReservationConvertedToSale`.
7. Relacionar el movimiento con `product_hold_id`.

No se debe disminuir `available_quantity` en este paso porque ya fue disminuido al crear la reserva.

## Ejemplo: dos tallas enviadas para escoger una

Situacion:

- Se envia vestido talla M.
- Se envia vestido talla L.
- La clienta escoge talla M.

Flujo:

1. Crear hold para M.
2. Crear hold para L.
3. Ambas prendas dejan de estar disponibles.
4. La clienta escoge M.
5. Hold M pasa a `ConvertedToSale`.
6. Hold L pasa a `NotSelected`.
7. Solo M aparece en `sale_products` como producto vendido.

## Regla: no usar `sale_products` para productos no vendidos

Productos enviados solo para seleccion no deben aparecer como vendidos.

Deben estar en `product_holds` y en `inventory_movements`, no en `sale_products`.

## Regla: no usar reservas para incidencias operativas

No usar `product_holds` para sacar de inventario productos danados, sucios, no encontrados o en reparacion.

Esos casos deben abrir un `product_inventory_issue` y afectar `unavailable_quantity`.

## Regla: actualizacion centralizada

Las reservas no deben modificar inventario desde cualquier parte del sistema.

Usar metodos centralizados como:

- `InventoryService.CreateHold()`
- `InventoryService.ReleaseHold()`
- `InventoryService.ConvertHoldToSale()`
