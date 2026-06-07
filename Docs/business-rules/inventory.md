# Inventory Business Rules

## Objetivo

Controlar las cantidades físicas y disponibles de los productos, manteniendo historial de todos los cambios relevantes.

## Tablas principales

- `products`
- `inventory_movements`
- `inventory_movement_types`
- `product_details`
- `sizes`
- `product_holds` *(si se implementa la opción de reservas explícitas)*

## Conceptos

### `quantity`

Cantidad total comprada en una línea de compra.

### `received_quantity`

Cantidad físicamente recibida. Si no existe aún en el esquema, se recomienda agregarla cuando se implemente recepción parcial de compras.

### `available_quantity`

Cantidad disponible para vender en tienda o canales digitales.

### `reserved_quantity`

Cantidad apartada temporalmente que ya no está disponible para venta, pero que todavía no se considera vendida. Si no existe aún en el esquema, se recomienda agregarla si se manejarán reservas.

## Regla: una venta confirmada descuenta inventario

Cuando una línea de venta se confirma:

1. Validar que `available_quantity >= quantity`.
2. Disminuir `products.available_quantity`.
3. Crear un registro en `inventory_movements` con tipo `Sale`.
4. Relacionar el movimiento con `sale_detail_id`.

## Regla: productos dañados o perdidos no son ventas

Los productos dañados o perdidos no deben registrarse como ventas de monto cero.

Deben registrarse como movimientos de inventario:

- `Damaged`
- `Lost`
- `ManualAdjustment`

Efecto esperado:

1. Disminuir `available_quantity`.
2. Crear `inventory_movement` con el tipo correspondiente.
3. Agregar comentario explicando el motivo.

## Regla: recepción de productos aumenta inventario disponible

Cuando se recibe producto de una orden:

1. Aumentar `received_quantity` si se implementa este campo.
2. Aumentar `available_quantity`.
3. Crear `inventory_movement` con tipo `PurchaseReceipt` o `Purchase`.
4. Relacionar el movimiento con `order_id`.

## Regla: reservas de producto

Aplica cuando un producto sale temporalmente de tienda, pero todavía no se considera vendido.

Ejemplo: se mandan dos tallas para que la clienta escoja una.

Al crear reserva:

1. Validar que `available_quantity >= quantity`.
2. Disminuir `available_quantity`.
3. Aumentar `reserved_quantity`.
4. Crear `product_hold` con estado `Active`, si se implementa esta tabla.
5. Crear `inventory_movement` con tipo `ReservationCreated`.

Al liberar reserva:

1. Cambiar `product_hold.status` a `Released`.
2. Disminuir `reserved_quantity`.
3. Aumentar `available_quantity`.
4. Crear `inventory_movement` con tipo `ReservationReleased`.

Al convertir reserva en venta:

1. Cambiar `product_hold.status` a `ConvertedToSale`.
2. Disminuir `reserved_quantity`.
3. Crear o confirmar `sale_detail`.
4. Crear `inventory_movement` con tipo `ReservationConvertedToSale`.

Importante: al convertir reserva en venta no se debe disminuir nuevamente `available_quantity`, porque ya se disminuyó al crear la reserva.

## Tipos recomendados para `inventory_movement_types`

- `PurchaseReceipt`
- `Sale`
- `Return`
- `ExchangeOut`
- `ExchangeIn`
- `Damaged`
- `Lost`
- `ReservationCreated`
- `ReservationReleased`
- `ReservationConvertedToSale`
- `ManualAdjustment`

## Reglas de consistencia

- `available_quantity` no puede ser menor que cero.
- `reserved_quantity` no puede ser menor que cero.
- No se debe modificar stock sin crear un `inventory_movement`.
- Los movimientos de inventario deben hacerse desde un único servicio, por ejemplo `InventoryService`.
