# Inventory Business Rules

## Objetivo

Controlar las cantidades compradas, recibidas, disponibles y reservadas de los productos, manteniendo un historial de todos los cambios relevantes en el inventario.

## Tablas principales

* `products`
* `inventory_movements`
* `inventory_movement_types`
* `product_details`
* `sizes`
* `product_holds`
* `product_hold_statuses`
* `orders`
* `sale_details`

## Conceptos

### `quantity`

Cantidad total comprada en una línea de una orden.

Este valor representa la cantidad esperada y no debe cambiar durante una recepción parcial.

### `received_quantity`

Cantidad físicamente recibida de la línea de compra.

Debe cumplirse:

`0 <= ReceivedQuantity <= Quantity`

### `available_quantity`

Cantidad actualmente disponible para vender o reservar.

No incluye unidades reservadas, vendidas, perdidas, dañadas o descartadas.

### `reserved_quantity`

Cantidad apartada temporalmente que no está disponible para otras ventas, pero que todavía no se considera vendida.

### `unit_cost_nio`

Costo unitario final del producto en córdobas.

Incluye:

* costo de la mercadería
* parte asignada del envío de importación

Las recepciones parciales no deben modificar este valor.

## Regla: todo cambio de inventario debe generar un movimiento

No se debe modificar `received_quantity`, `available_quantity` o `reserved_quantity` sin crear el registro correspondiente en `inventory_movements`.

El cambio de cantidades y la creación del movimiento deben ejecutarse dentro de la misma transacción.

Si una de las operaciones falla, ninguna debe persistirse.

Los cambios de inventario deben realizarse desde un único servicio de aplicación, por ejemplo:

`InventoryService`

Los controladores y otros servicios no deben modificar directamente las cantidades de `products`.

## Regla: recepción de productos

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

Ejemplo:

* Cantidad comprada: 10
* Primera recepción: 6
* Segunda recepción: 4

Después de la primera recepción:

* `received_quantity = 6`
* `available_quantity = 6`

Después de la segunda recepción, si no hubo ventas ni reservas:

* `received_quantity = 10`
* `available_quantity = 10`

## Regla: una recepción parcial no modifica el costo

Una recepción parcial solo modifica las cantidades recibidas y disponibles.

No debe modificar:

* `merchandise_total_cost_nio`
* `allocated_shipping_cost_nio`
* `total_cost_nio`
* `unit_cost_nio`

Las recepciones parciales no generan lotes de costo independientes.

El costo unitario se calcula usando el costo total asignado y la cantidad total comprada:

`UnitCostNio = TotalCostNio / Quantity`

Si el proveedor confirma que una parte de la línea nunca será entregada, la cantidad definitiva y los costos de la línea deben ajustarse antes de cerrar la orden.

## Regla: no vender productos no recibidos

El sistema solamente puede vender desde `available_quantity`.

Unidades compradas pero todavía no recibidas no deben estar disponibles para venta ni reserva.

Antes de vender:

`AvailableQuantity >= RequestedQuantity`

## Regla: una venta descuenta inventario

Cuando se confirma una línea de venta que no proviene de una reserva:

1. Validar que `available_quantity` sea suficiente.
2. Disminuir `available_quantity`.
3. Crear un `inventory_movement` de tipo `Sale`.
4. Relacionar el movimiento con `sale_detail_id`.
5. Copiar `products.unit_cost_nio` a `sale_details.unit_cost_at_sale`.

Debe cumplirse:

`AvailableQuantity >= SaleQuantity`

El movimiento debe registrar una cantidad positiva. El tipo del movimiento determina que su efecto sobre el inventario es una salida.

## Regla: cancelación de una venta

Si una línea vendida es cancelada y el producto puede volver al inventario:

1. Cambiar el estado de la línea de venta.
2. Aumentar `available_quantity`.
3. Crear un `inventory_movement` de tipo `SaleCancelled`.
4. Relacionar el movimiento con la línea de venta original.

No se debe eliminar el movimiento de venta original.

## Regla: devolución de producto

Cuando una clienta devuelve un producto en condiciones de volver a venderse:

1. Cambiar el estado de la línea original.
2. Aumentar `available_quantity`.
3. Crear un `inventory_movement` de tipo `CustomerReturn`.
4. Relacionar el movimiento con la línea de venta original.

Si el producto devuelto no puede volver a venderse, no debe aumentarse directamente `available_quantity`. Debe registrarse como dañado o descartado según corresponda.

## Regla: cambio de producto

Cuando una clienta cambia un producto:

1. El producto original entra mediante un movimiento `ExchangeReturn`, siempre que pueda volver a venderse.
2. El producto nuevo sale mediante un movimiento `Sale`.
3. La línea original cambia a estado `Exchanged`.
4. La nueva línea se agrega a la misma venta.
5. Cada movimiento debe relacionarse con la línea de venta correspondiente.

No se necesita un tipo `ExchangeSale`, porque la salida del producto nuevo representa una venta.

## Regla: reservas de productos

Una reserva representa inventario separado temporalmente que todavía no se considera vendido.

Ejemplo: se envían dos tallas para que la clienta escoja una.

### Crear reserva

1. Validar que la cantidad solicitada sea mayor que cero.
2. Validar que `available_quantity` sea suficiente.
3. Disminuir `available_quantity`.
4. Aumentar `reserved_quantity`.
5. Crear un `product_hold` con estado `Active`.
6. Crear un `inventory_movement` de tipo `ReservationCreated`.

Cambios:

`AvailableQuantity -= HoldQuantity`

`ReservedQuantity += HoldQuantity`

### Liberar producto no seleccionado

Cuando la clienta no selecciona el producto:

1. Cambiar el estado del `product_hold` a `NotSelected`.
2. Disminuir `reserved_quantity`.
3. Aumentar `available_quantity`.
4. Crear un `inventory_movement` de tipo `ReservationReleased`.

Cambios:

`ReservedQuantity -= HoldQuantity`

`AvailableQuantity += HoldQuantity`

### Convertir reserva en venta

Cuando la clienta selecciona el producto:

1. Cambiar el estado del `product_hold` a `ConvertedToSale`.
2. Disminuir `reserved_quantity`.
3. Crear o confirmar el `sale_detail`.
4. Crear un `inventory_movement` de tipo `ReservationConvertedToSale`.
5. Copiar el costo actual del producto a `sale_details.unit_cost_at_sale`.

Cambio:

`ReservedQuantity -= HoldQuantity`

No se debe volver a disminuir `available_quantity`, porque ya se disminuyó cuando se creó la reserva.

## Regla: producto dañado

Cuando un producto disponible se daña:

1. Validar que exista suficiente cantidad disponible.
2. Disminuir `available_quantity`.
3. Crear un `inventory_movement` de tipo `Damaged`.
4. Registrar el motivo o comentario.

Un producto dañado no debe registrarse como una venta con monto cero.

## Regla: producto reparado

Cuando un producto previamente dañado es reparado y puede volver a venderse:

1. Aumentar `available_quantity`.
2. Crear un `inventory_movement` de tipo `Repaired`.
3. Relacionar, cuando sea posible, el movimiento con el producto y el antecedente del daño.
4. Registrar un comentario explicando la reparación.

No se debe utilizar `AdjustmentIncrease` cuando la causa conocida sea una reparación.

## Regla: producto perdido

Cuando un producto disponible se considera perdido:

1. Validar que exista suficiente cantidad disponible.
2. Disminuir `available_quantity`.
3. Crear un `inventory_movement` de tipo `Lost`.
4. Registrar el motivo o comentario.

Un producto perdido no debe registrarse como una venta con monto cero.

## Regla: producto encontrado

Cuando aparece un producto previamente registrado como perdido:

1. Aumentar `available_quantity`.
2. Crear un `inventory_movement` de tipo `Found`.
3. Relacionar, cuando sea posible, el movimiento con el antecedente de pérdida.
4. Registrar un comentario.

No se debe utilizar `AdjustmentIncrease` cuando la causa conocida sea que el producto fue encontrado.

## Regla: producto descartado

Un producto debe registrarse como `Discarded` cuando se determina que no podrá recuperarse ni venderse.

Si el producto todavía estaba disponible:

1. Disminuir `available_quantity`.
2. Crear un `inventory_movement` de tipo `Discarded`.

Si el producto ya había salido de disponible por estar dañado o perdido:

1. No volver a disminuir `available_quantity`.
2. Crear el movimiento `Discarded` para dejar constancia de su disposición definitiva.

El movimiento debe conservar el costo del producto utilizado para calcular la pérdida de inventario.

El descarte no representa una salida de efectivo en ese momento y no debe registrarse como una venta.

## Regla: ajustes manuales

Los ajustes manuales deben utilizarse solamente cuando la diferencia no tenga una causa específica representada por otro tipo de movimiento.

### Ajuste positivo

Utilizar `AdjustmentIncrease` cuando un conteo físico encuentre más unidades que las registradas y no exista una causa más específica.

Efecto:

`AvailableQuantity += AdjustmentQuantity`

### Ajuste negativo

Utilizar `AdjustmentDecrease` cuando un conteo físico encuentre menos unidades que las registradas y no exista una causa más específica.

Efecto:

`AvailableQuantity -= AdjustmentQuantity`

Todo ajuste manual debe incluir un comentario obligatorio.

No debe utilizarse un ajuste genérico para representar:

* ventas
* devoluciones
* productos encontrados
* productos reparados
* daños
* pérdidas
* descartes
* reservas

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
* `ReservationCreated`
* `ReservationReleased`
* `ReservationConvertedToSale`

## Datos mínimos de un movimiento de inventario

Cada movimiento debe guardar al menos:

* producto
* tipo de movimiento
* cantidad
* fecha UTC
* costo unitario histórico cuando sea relevante
* usuario que realizó la operación
* comentario o motivo cuando corresponda
* referencia a la orden, venta, línea de venta o reserva cuando aplique

La cantidad debe almacenarse como un valor positivo. El tipo de movimiento determina si aumenta, disminuye o transfiere cantidades entre disponible y reservado.

## Reglas de consistencia

Debe cumplirse:

* `quantity > 0`
* `received_quantity >= 0`
* `received_quantity <= quantity`
* `available_quantity >= 0`
* `reserved_quantity >= 0`
* `available_quantity + reserved_quantity <= received_quantity`

Además:

* No se debe modificar inventario sin crear un `inventory_movement`.
* Los movimientos históricos no deben eliminarse.
* Una reserva convertida en venta no debe descontar dos veces el inventario disponible.
* Las modificaciones de cantidades y movimientos deben guardarse en la misma transacción.
* Las fechas deben almacenarse en UTC.
* El costo histórico de una venta no debe depender de cambios futuros en `products`.
