# Purchases Business Rules

## Objetivo

Registrar compras a proveedores, tracking numbers y recepción de productos sin obligar a dividir productos por caja o tracking.

## Tablas principales

- `orders`
- `order_statuses`
- `order_tracking_numbers`
- `suppliers`
- `shipping_companies`
- `products`
- `inventory_movements`

## Regla: una orden representa una compra al proveedor

Una orden agrupa productos comprados a un proveedor.

`orders` debe contener:

- proveedor
- fecha de orden
- estado
- monto
- monto USD
- costo de envío si aplica
- comentarios

## Regla: un tracking representa logística, no detalle de productos

Los tracking numbers sirven para controlar paquetes o cajas recibidas, pero no se debe obligar a indicar qué productos venían en cada tracking.

Razón: operativamente se abren todas las cajas y luego se separa por producto.

Por tanto:

- `order_tracking_numbers` registra los paquetes.
- `products` registra lo comprado por producto/talla/color.
- La recepción real se registra por producto, no por tracking.

## Regla: una orden puede tener varios tracking numbers

Una orden puede tener uno o varios registros en `order_tracking_numbers`.

Cada tracking puede tener:

- compañía de envío
- número de tracking
- fecha de entrega
- peso
- costo de envío
- estado de entregado

## Regla: recepción parcial de compra

Si una orden llega en varias fechas, el sistema debe permitir recibir cantidades parciales por producto.

Recomendación de campo:

- `products.received_quantity`

Flujo:

1. Crear orden con productos comprados.
2. Inicializar `received_quantity = 0`.
3. Cuando se recibe producto, aumentar `received_quantity`.
4. Aumentar `available_quantity`.
5. Crear `inventory_movement` relacionado a la orden.

Ejemplo:

- Comprado: 10 unidades.
- Recibido primer día: 6 unidades.
- Recibido segundo día: 4 unidades.

## Regla: no vender productos no recibidos

El sistema solo debe vender desde `available_quantity`.

Si un producto está comprado pero no recibido, no debe estar disponible para venta.

## Regla: estado de orden

Estados sugeridos para `order_statuses`:

- `Pending`
- `PartiallyReceived`
- `Received`
- `Cancelled`

La orden debe pasar a `Received` solamente cuando todos sus productos hayan sido recibidos completamente.
