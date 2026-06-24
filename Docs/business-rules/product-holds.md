# Product Holds Business Rules

## Objetivo

Registrar productos enviados temporalmente a una clienta para seleccion, prueba o talla, sin registrarlos todavia como vendidos.

Este modulo no cubre reservas con pago previo. Si una clienta paga parcial o totalmente para retiro o envio futuro, se debe crear una venta con `sales.sale_status_id = Reserved`.

No cubre productos danados, sucios, perdidos o en revision. Esos casos se manejan con `product_inventory_issues`.

## Tablas principales

- `product_holds`
- `product_hold_statuses`
- `products`
- `inventory_movements`
- `sales`
- `sale_products`

## Responsabilidad de `product_holds`

`product_holds` representa el estado actual e historico de productos que salen temporalmente de disponibilidad por un flujo comercial de seleccion o prueba.

Sirve para responder:

- Que productos estan temporalmente fuera de disponibilidad por seleccion?
- A que clienta o venta potencial pertenecen, si aplica?
- Cuales fueron seleccionados?
- Cuales regresaron a inventario disponible?

No debe usarse para apartar productos por pago pendiente o reserva de clienta. Esos casos se modelan como ventas en estado `Reserved`.

## Estados

- `Active`
- `NotSelected`
- `ConvertedToSale`

## Razones

- `SentForSelection`

## Regla: crear hold de seleccion

Cuando se envia o aparta producto para seleccion/prueba/talla:

1. Validar que `available_quantity >= quantity`.
2. Crear `product_hold` con estado `Active` y razon `SentForSelection`.
3. Disminuir `products.available_quantity`.
4. Aumentar `products.unavailable_quantity`.
5. Crear `inventory_movement` relacionado con `product_hold_id`.

El producto no queda reservado para una venta confirmada; queda temporalmente no disponible porque esta fuera de tienda o en seleccion.

## Regla: liberar hold no seleccionado

Cuando el producto regresa a tienda o la clienta no lo selecciona:

1. Validar que el hold este `Active`.
2. Cambiar `product_hold.status` a `NotSelected`.
3. Colocar `resolved_at`.
4. Disminuir `products.unavailable_quantity`.
5. Aumentar `products.available_quantity`.
6. Crear `inventory_movement` relacionado con `product_hold_id`.

## Regla: convertir hold en venta

Cuando la clienta escoge el producto enviado para seleccion:

1. Validar que el hold este `Active`.
2. Crear o confirmar `sale_product`.
3. Cambiar `product_hold.status` a `ConvertedToSale`.
4. Colocar `resolved_at`.
5. Disminuir `products.unavailable_quantity`.
6. Crear `inventory_movement` relacionado con `product_hold_id` y/o `sale_product_id`.

No se debe disminuir `available_quantity` en este paso porque ya fue disminuido al crear el hold.

## Ejemplo: dos tallas enviadas para escoger una

Situacion:

- Se envia vestido talla M.
- Se envia vestido talla L.
- La clienta escoge talla M.

Flujo:

1. Crear hold para M.
2. Crear hold para L.
3. Ambas prendas dejan de estar disponibles y aumentan `unavailable_quantity`.
4. La clienta escoge M.
5. Hold M pasa a `ConvertedToSale` y disminuye `unavailable_quantity`.
6. Hold L pasa a `NotSelected`, disminuye `unavailable_quantity` y aumenta `available_quantity`.
7. Solo M aparece en `sale_products` como producto vendido.

## Regla: no usar `sale_products` para productos no vendidos

Productos enviados solo para seleccion no deben aparecer como vendidos.

Deben estar en `product_holds` y en `inventory_movements`, no en `sale_products`.

## Regla: no usar holds para reservas con pago

No usar `product_holds` para productos que ya tienen pago parcial o total.

Si hay pago previo, crear una venta en estado `Reserved` y registrar el pago en `sale_payments`.

## Regla: no usar holds para incidencias operativas

No usar `product_holds` para sacar de inventario productos danados, sucios, no encontrados o en reparacion.

Esos casos deben abrir un `product_inventory_issue` y afectar `unavailable_quantity`.

## Regla: actualizacion centralizada

Los holds no deben modificar inventario desde cualquier parte del sistema.

Usar metodos centralizados como:

- `InventoryService.CreateSelectionHold()`
- `InventoryService.ReleaseSelectionHold()`
- `InventoryService.ConvertSelectionHoldToSale()`
