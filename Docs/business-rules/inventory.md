# Inventory Business Rules

## Objetivo

Controlar las cantidades compradas, recibidas, disponibles, reservadas y no disponibles de los productos, manteniendo historial de todos los cambios relevantes en inventario.

## Tablas principales

* `products`
* `inventory_movements`
* `inventory_movement_types`
* `product_details`
* `sizes`
* `product_holds`
* `product_hold_statuses`
* `product_inventory_issues`
* `product_inventory_issue_types`
* `product_inventory_issue_statuses`
* `orders`
* `sale_products`

## Conceptos

### `quantity`

Cantidad total comprada en una linea de una orden.

Este valor representa la cantidad esperada y no debe cambiar durante una recepcion parcial.

### `received_quantity`

Cantidad fisicamente recibida de la linea de compra.

Debe cumplirse:

`0 <= ReceivedQuantity <= Quantity`

### `available_quantity`

Cantidad actualmente disponible para vender o reservar.

No incluye unidades reservadas, vendidas, no disponibles temporalmente, perdidas definitivamente o descartadas.

### `reserved_quantity`

Cantidad comprometida por ventas reservadas.

No se maneja con `product_holds`. Si hay pago previo, la reserva debe existir como una venta con estado `Reserved`.

### `unavailable_quantity`

Cantidad fisicamente existente, pero no vendible temporalmente por una condicion operativa o por seleccion de talla.

Ejemplos:

* producto enviado para seleccion o prueba de talla
* producto dañado
* producto sucio
* producto no encontrado fisicamente, pero pendiente de busqueda
* producto en revision
* producto en reparacion

Los productos enviados para seleccion se manejan con `product_holds`. Los productos danados, sucios, perdidos o en revision se manejan con `product_inventory_issues`.

### `unit_cost_nio`

Costo unitario final del producto en cordobas.

Incluye:

* costo de la mercaderia
* parte asignada del envio de importacion

Las recepciones parciales no deben modificar este valor.

## Regla: todo cambio de inventario debe generar un movimiento

No se debe modificar `received_quantity`, `available_quantity`, `reserved_quantity` o `unavailable_quantity` sin crear el registro correspondiente en `inventory_movements`.

El cambio de cantidades y la creacion del movimiento deben ejecutarse dentro de la misma transaccion.

Si una de las operaciones falla, ninguna debe persistirse.

Los cambios de inventario deben realizarse desde un unico servicio de aplicacion, por ejemplo:

`InventoryService`

Los controladores y otros servicios no deben modificar directamente las cantidades de `products`.

## Regla: recepcion de productos

Cuando se recibe una cantidad de producto de una orden:

1. Validar que la cantidad recibida sea mayor que cero.
2. Validar que la nueva cantidad recibida no supere `quantity`.
3. Aumentar `received_quantity`.
4. Aumentar `available_quantity`.
5. Crear un `inventory_movement` de tipo `PurchaseReceived`.
6. Relacionar el movimiento con el producto y la orden.
7. Actualizar el estado de la orden cuando corresponda.

Debe cumplirse:

`ReceivedQuantity + QuantityToReceive <= Quantity`

## Regla: no vender productos no recibidos

El sistema solamente puede vender desde `available_quantity`.

Unidades compradas pero todavia no recibidas no deben estar disponibles para venta ni reserva.

Antes de vender:

`AvailableQuantity >= RequestedQuantity`

## Regla: una venta descuenta inventario

Cuando se confirma una linea de venta que no proviene de una reserva:

1. Validar que `available_quantity` sea suficiente.
2. Disminuir `available_quantity`.
3. Crear un `inventory_movement` de tipo `Sale`.
4. Relacionar el movimiento con `sale_product_id`.
5. Copiar `products.unit_cost_nio` a `sale_products.unit_cost_at_sale`.

Debe cumplirse:

`AvailableQuantity >= SaleQuantity`

El movimiento debe registrar una cantidad positiva. El tipo del movimiento determina que su efecto sobre el inventario es una salida.

## Regla: cancelacion de una venta

Si una linea vendida es cancelada y el producto puede volver al inventario:

1. Cambiar el estado de la linea de venta.
2. Aumentar `available_quantity`.
3. Crear un `inventory_movement` de tipo `SaleCancelled`.
4. Relacionar el movimiento con la linea de venta original.

No se debe eliminar el movimiento de venta original.

## Regla: devolucion de producto

Cuando una clienta devuelve un producto en condiciones de volver a venderse:

1. Cambiar el estado de la linea original.
2. Aumentar `available_quantity`.
3. Crear un `inventory_movement` de tipo `CustomerReturn`.
4. Relacionar el movimiento con la linea de venta original.

Si el producto devuelto no puede volver a venderse, no debe aumentar directamente `available_quantity`. Debe abrirse un `product_inventory_issue` o registrarse como `Discarded`, segun corresponda.

## Regla: cambio de producto

Cuando una clienta cambia un producto:

1. El producto original entra mediante un movimiento `ExchangeReturn`, siempre que pueda volver a venderse.
2. El producto nuevo sale mediante un movimiento `Sale`.
3. La linea original cambia a estado `Exchanged`.
4. La nueva linea se agrega a la misma venta.
5. Cada movimiento debe relacionarse con la linea de venta correspondiente.

No se necesita un tipo `ExchangeSale`, porque la salida del producto nuevo representa una venta.

## Regla: holds de seleccion

Un hold de seleccion representa inventario separado temporalmente porque una clienta esta probando, tallando o escogiendo entre varias opciones.

Los holds de seleccion usan `product_holds` y afectan `unavailable_quantity`, no `reserved_quantity`.

Si hay pago previo, no se debe usar `product_holds`; se debe crear una venta con estado `Reserved`.

### Crear hold de seleccion

1. Validar que la cantidad solicitada sea mayor que cero.
2. Validar que `available_quantity` sea suficiente.
3. Disminuir `available_quantity`.
4. Aumentar `unavailable_quantity`.
5. Crear un `product_hold` con estado `Active` y razon `SentForSelection`.
6. Crear un `inventory_movement` relacionado con `product_hold_id`.

Cambios:

`AvailableQuantity -= HoldQuantity`

`UnavailableQuantity += HoldQuantity`

### Liberar producto no seleccionado

Cuando la clienta no selecciona el producto y este regresa disponible:

1. Cambiar el estado del `product_hold` a `NotSelected`.
2. Disminuir `unavailable_quantity`.
3. Aumentar `available_quantity`.
4. Crear un `inventory_movement` relacionado con `product_hold_id`.

Cambios:

`UnavailableQuantity -= HoldQuantity`

`AvailableQuantity += HoldQuantity`

### Convertir hold en venta

Cuando la clienta selecciona el producto:

1. Cambiar el estado del `product_hold` a `ConvertedToSale`.
2. Disminuir `unavailable_quantity`.
3. Crear o confirmar el `sale_product`.
4. Crear un `inventory_movement` relacionado con `product_hold_id` y/o `sale_product_id`.
5. Copiar el costo actual del producto a `sale_products.unit_cost_at_sale`.

Cambio:

`UnavailableQuantity -= HoldQuantity`

No se debe volver a disminuir `available_quantity`, porque ya se disminuyo cuando se creo el hold.

## Regla: producto temporalmente no disponible

Cuando un producto disponible no puede venderse temporalmente por dano, suciedad, extravio pendiente o revision:

1. Validar que `available_quantity` sea suficiente.
2. Crear `product_inventory_issue` con estado `Open`.
3. Disminuir `available_quantity`.
4. Aumentar `unavailable_quantity`.
5. Crear `inventory_movement` con el tipo especifico, por ejemplo `Damaged` o `Lost`.
6. Relacionar el movimiento con `product_inventory_issue_id`.

Cambios:

`AvailableQuantity -= IssueQuantity`

`UnavailableQuantity += IssueQuantity`

No se debe usar `product_holds` para este caso.

## Regla: producto reparado o encontrado

Cuando un producto no disponible vuelve a ser vendible:

1. Validar que el `product_inventory_issue` este `Open`.
2. Cambiar estado a `ResolvedToAvailable`.
3. Colocar `resolved_at`.
4. Disminuir `unavailable_quantity`.
5. Aumentar `available_quantity`.
6. Crear `inventory_movement` de tipo `Repaired` o `Found`.
7. Relacionar el movimiento con `product_inventory_issue_id`.

Cambios:

`UnavailableQuantity -= IssueQuantity`

`AvailableQuantity += IssueQuantity`

No se debe utilizar `AdjustmentIncrease` cuando la causa conocida sea reparacion o hallazgo.

## Regla: producto descartado o perdida confirmada

Cuando se determina que una unidad no podra recuperarse ni venderse:

1. Validar si la unidad esta disponible o ya esta en un issue operativo.
2. Si esta disponible, disminuir `available_quantity`.
3. Si esta en `unavailable_quantity`, disminuir `unavailable_quantity`.
4. Crear `inventory_movement` de tipo `Discarded` o `Lost`.
5. Si existe issue, cambiarlo a `Discarded` o `ConfirmedLost` y relacionar el movimiento con `product_inventory_issue_id`.

No se debe descontar dos veces `available_quantity` si la unidad ya habia salido de disponible al abrir el issue.

El movimiento debe conservar el costo del producto utilizado para calcular la perdida de inventario.

El descarte no representa una salida de efectivo en ese momento y no debe registrarse como una venta.

## Regla: ajustes manuales

Los ajustes manuales deben utilizarse solamente cuando la diferencia no tenga una causa especifica representada por otro tipo de movimiento o issue.

### Ajuste positivo

Utilizar `AdjustmentIncrease` cuando un conteo fisico encuentre mas unidades que las registradas y no exista una causa mas especifica.

Efecto:

`AvailableQuantity += AdjustmentQuantity`

### Ajuste negativo

Utilizar `AdjustmentDecrease` cuando un conteo fisico encuentre menos unidades que las registradas y no exista una causa mas especifica.

Efecto:

`AvailableQuantity -= AdjustmentQuantity`

Todo ajuste manual debe incluir un comentario obligatorio.

No debe utilizarse un ajuste generico para representar:

* ventas
* devoluciones
* productos encontrados
* productos reparados
* danos
* perdidas
* descartes
* reservas con pago
* holds de seleccion
* issues de inventario

## Tipos recomendados para `inventory_movement_types`

* `PurchaseReceived`
* `Sale`
* `SaleCancelled`
* `CustomerReturn`
* `ExchangeReturn`
* `Damaged`
* `Repaired`
* `Lost`
* `Found`
* `Discarded`
* `AdjustmentIncrease`
* `AdjustmentDecrease`
* `ReservationCreated` (hold de seleccion creado; nombre pendiente de normalizar)
* `ReservationReleased` (hold de seleccion liberado; nombre pendiente de normalizar)
* `ReservationConvertedToSale` (hold de seleccion convertido; nombre pendiente de normalizar)

## Datos minimos de un movimiento de inventario

Cada movimiento debe guardar al menos:

* producto
* tipo de movimiento
* cantidad
* fecha UTC
* costo unitario historico cuando sea relevante
* usuario que realizo la operacion
* comentario o motivo cuando corresponda
* referencia a la orden, venta, linea de venta, hold de seleccion o issue cuando aplique

La cantidad debe almacenarse como un valor positivo. El tipo del movimiento determina si aumenta, disminuye o transfiere cantidades entre disponible, reservado y no disponible.

## Reglas de consistencia

Debe cumplirse:

* `quantity > 0`
* `received_quantity >= 0`
* `received_quantity <= quantity`
* `available_quantity >= 0`
* `reserved_quantity >= 0`
* `unavailable_quantity >= 0`
* `available_quantity + reserved_quantity + unavailable_quantity <= received_quantity`

Ademas:

* No se debe modificar inventario sin crear un `inventory_movement`.
* Los movimientos historicos no deben eliminarse.
* Un hold de seleccion convertido en venta no debe descontar dos veces el inventario disponible.
* Un issue descartado o perdido no debe descontar dos veces el inventario disponible.
* Las modificaciones de cantidades y movimientos deben guardarse en la misma transaccion.
* Las fechas deben almacenarse en UTC.
* El costo historico de una venta o perdida no debe depender de cambios futuros en `products`.
