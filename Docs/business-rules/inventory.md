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

## Regla: ajustes manuales de inventario

Los ajustes manuales se registran en `inventory_adjustments` y `inventory_adjustment_items`. Cada item debe crear exactamente un `inventory_movement` mediante `InventoryService`.

Un ajuste aplica cuando se necesita corregir una diferencia operativa que no pertenece naturalmente a una venta, devolucion, cambio, recepcion de compra, seleccion o issue abierto.

Ejemplos:

* cruce de codigo entre productos
* correccion manual de conteo
* faltante detectado despues de haber recibido inventario
* recuperacion de un producto previamente dado de baja
* donacion o salida no comercial

Cada item debe indicar:

* `product_id`
* `from_stock_bucket_id`
* `to_stock_bucket_id`
* `quantity`
* comentario opcional

El encabezado del ajuste puede guardar `reference` y `comments`:

* `reference`: identificador corto para rastrear el origen del ajuste, como folio, orden, factura, conteo fisico, venta relacionada o codigo interno.
* `comments`: explicacion en lenguaje humano sobre que paso, por que se ajusto y cualquier detalle util para auditoria.

Ejemplos de `reference`: `CONTEO-JULIO-2026`, `ORDEN-123`, `FACTURA-9981`, `VENTA-456`.

Ejemplos de `comments`: `Sobrante encontrado al recibir compra.`, `Se corrigio cruce de codigo entre dos variantes.`, `Conteo fisico mostro una unidad menos y no se encontro flujo asociado.`

Debe cumplirse:

`from_stock_bucket_id <> to_stock_bucket_id`

`quantity > 0`

Los motivos del ajuste viven en `inventory_adjustment_reasons`. El motivo explica por que se hace el ajuste; el movimiento explica que bucket cambia.

Los ajustes usan un unico tipo de movimiento:

* `AdjustmentTransfer`: registra el cambio de bucket del item de ajuste, por ejemplo `External -> Available`, `Available -> OutOfInventory` o `Available -> Unavailable`.

La causa del ajuste no se infiere del tipo de movimiento; vive en `inventory_adjustment_reasons`.

Un ajuste positivo desde `External` aumenta `received_quantity` y no puede dejar `received_quantity > quantity`. Si el sobrante recibido supera la cantidad comprada, primero debe corregirse la compra/costo de la variante antes de registrar la entrada de inventario.

No se debe usar ajuste manual cuando existe un flujo especifico:

* producto danado, sucio o en revision: usar `product_inventory_issues`
* venta, cancelacion de venta o envio: usar el flujo de ventas/envios
* devolucion o cambio de clienta: usar `sale_returns` o `sale_exchanges`
* recepcion normal de compra: usar `order_receipts`

## Regla: recepcion de productos

Cuando se recibe una cantidad de producto de una orden:

1. Validar que la cantidad recibida sea mayor que cero.
2. Validar que la nueva cantidad recibida no supere `quantity`.
3. Aumentar `received_quantity`.
4. Aumentar `available_quantity`.
5. Crear un `inventory_movement` de tipo `PurchaseReceived` con `External -> Available`.
6. Relacionar el movimiento con el producto y la orden.
7. Actualizar el estado de la orden cuando corresponda.

Debe cumplirse:

`ReceivedQuantity + QuantityToReceive <= Quantity`

## Regla: no vender productos no recibidos

El sistema solamente puede vender desde `available_quantity`.

Unidades compradas pero todavia no recibidas no deben estar disponibles para venta ni reserva.

Antes de vender:

`AvailableQuantity >= RequestedQuantity`

## Regla: ciclo de inventario de una venta

El bucket depende del estado operativo de la venta:

* `Pending`: no compromete inventario.
* `Reserved` y `ReadyForDelivery`: cada línea permanece en `Reserved` mediante `Available -> Reserved` y `ReservationCreated`.
* `SentForDelivery` y `Completed`: cada línea permanece en `OutOfInventory`.
* Al despachar un envío, usar `Reserved -> OutOfInventory` y `ReservationConvertedToSale`.
* Si el envío falla, usar `OutOfInventory -> Reserved` solamente por la salida neta que todavía continúa fuera.
* No se puede fallar el envío mientras una devolución aún no haya sido recibida físicamente o un cambio solicitado aún no haya registrado su entrega física; esas unidades todavía dependen de permanecer en `OutOfInventory`.

Una venta completada directamente en local puede usar `Available -> OutOfInventory` con tipo `Sale`. Todos los movimientos deben conservar `sale_product_id`.

Antes de reservar o vender debe cumplirse:

`AvailableQuantity >= SaleQuantity`

Una devolución recibida también conserva `sale_product_id`, además de su referencia a `sale_return_item_id` o `exchange_return_item_id`, para que la salida neta de la línea permanezca trazable.

Al corregir posteriormente los productos de una venta, el compromiso objetivo de cada línea es su cantidad comercial menos las unidades ya recibidas mediante devoluciones o cambios. Una corrección de precio no debe volver a reservar esas unidades retornadas.

## Regla: cancelacion de una venta

Si una linea vendida es cancelada y el producto puede volver al inventario:

1. Cambiar el estado de la venta a `Cancelled`.
2. Mover la cantidad neta comprometida desde `Reserved` o `OutOfInventory` hacia `Available`.
3. Crear un `inventory_movement` de tipo `ReservationReleased` o `SaleCancelled`, según el bucket de origen.
4. Relacionar el movimiento con la linea de venta original.

No se debe eliminar el movimiento de venta original.

## Regla: devolucion de producto

Cuando una clienta devuelve un producto en condiciones de volver a venderse:

1. Registrar el ítem en `sale_return_items`, vinculado a la línea original.
2. Aumentar `available_quantity` al recibirlo si está en condiciones de venta.
3. Crear un `inventory_movement` de tipo `CustomerReturn`.
4. Relacionar el movimiento con el ítem de devolución y conservar la referencia a la línea original.

Si el producto devuelto no puede volver a venderse, no debe aumentar directamente `available_quantity`. Debe abrirse un `product_inventory_issue` o registrarse como `Discarded`, segun corresponda.

## Regla: cambio de producto

Cuando una clienta cambia un producto:

1. El producto original se registra en `exchange_return_items` y entra mediante un movimiento `ExchangeReturnReceivedByAgency` cuando la agencia lo recibe.
2. El producto nuevo se registra en `exchange_outbound_items`: se reserva y luego sale mediante `ExchangeReplacementDelivered`.
3. La venta y sus líneas originales permanecen inmutables; el estado del cambio se conserva en `sale_exchanges` y el de cada retorno en `exchange_return_items`.
4. Cada movimiento se relaciona con el ítem de cambio correspondiente.

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
6. Crear un `inventory_movement` relacionado con `product_hold_id` usando `Available -> Unavailable`.

Cambios:

`AvailableQuantity -= HoldQuantity`

`UnavailableQuantity += HoldQuantity`

### Liberar producto no seleccionado

Cuando la clienta no selecciona el producto y este regresa disponible:

1. Cambiar el estado del `product_hold` a `NotSelected`.
2. Disminuir `unavailable_quantity`.
3. Aumentar `available_quantity`.
4. Crear un `inventory_movement` relacionado con `product_hold_id` usando `Unavailable -> Available`.

Cambios:

`UnavailableQuantity -= HoldQuantity`

`AvailableQuantity += HoldQuantity`

### Convertir hold en venta

Cuando la clienta selecciona el producto:

1. Cambiar el estado del `product_hold` a `ConvertedToSale`.
2. Disminuir `unavailable_quantity`.
3. Crear o confirmar el `sale_product`.
4. Crear un `inventory_movement` relacionado con `product_hold_id` y/o `sale_product_id` usando `Unavailable -> OutOfInventory`.
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
5. Crear `inventory_movement` con el tipo especifico, por ejemplo `Damaged` o `Lost`, usando `Available -> Unavailable`.
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
6. Crear `inventory_movement` de tipo `Repaired` o `Found` usando `Unavailable -> Available`.
7. Relacionar el movimiento con `product_inventory_issue_id`.

Cambios:

`UnavailableQuantity -= IssueQuantity`

`AvailableQuantity += IssueQuantity`

No se debe registrar un ajuste manual cuando la causa conocida sea reparacion o hallazgo.

## Regla: producto descartado o perdida confirmada

Cuando se determina que una unidad no podra recuperarse ni venderse:

1. Validar si la unidad esta disponible o ya esta en un issue operativo.
2. Si esta disponible, disminuir `available_quantity`.
3. Si esta en `unavailable_quantity`, disminuir `unavailable_quantity`.
4. Crear `inventory_movement` de tipo `Discarded` o `Lost` usando `Available -> OutOfInventory` o `Unavailable -> OutOfInventory`, segun el bucket actual.
5. Si existe issue, cambiarlo a `Discarded` o `ConfirmedLost` y relacionar el movimiento con `product_inventory_issue_id`.

No se debe descontar dos veces `available_quantity` si la unidad ya habia salido de disponible al abrir el issue.

El movimiento debe conservar el costo del producto utilizado para calcular la perdida de inventario.

El descarte no representa una salida de efectivo en ese momento y no debe registrarse como una venta.

## Regla: ajustes manuales

Los ajustes manuales deben utilizarse solamente cuando la diferencia no tenga una causa especifica representada por otro tipo de movimiento o issue.

### Ajuste positivo

Utilizar un ajuste con motivo `ManualCorrection` cuando un conteo fisico encuentre mas unidades que las registradas y no exista una causa mas especifica.

Efecto:

`AvailableQuantity += AdjustmentQuantity`

### Ajuste negativo

Utilizar un ajuste con motivo `ManualCorrection` cuando un conteo fisico encuentre menos unidades que las registradas y no exista una causa mas especifica.

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
* `IssueOpened`
* `IssueReturnedToAvailable`
* `IssueRemovedFromInventory`
* `ReservationCreated`
* `ReservationReleased`
* `ReservationConvertedToSale`
* `SelectionSent`
* `SelectionConvertedToSale`
* `SelectionReturned`
* `ExchangeReplacementReserved`
* `ExchangeReplacementDelivered`
* `ExchangeReplacementReservationReleased`
* `ExchangeReturnReceivedByAgency`
* `ExchangeReturnMissing`
* `AdjustmentTransfer`

## Datos minimos de un movimiento de inventario

Cada movimiento debe guardar al menos:

* producto
* tipo de movimiento
* bucket origen
* bucket destino
* cantidad
* fecha UTC
* costo unitario historico cuando sea relevante
* usuario que realizo la operacion
* comentario o motivo cuando corresponda
* referencia a la orden, venta, linea de venta, hold de seleccion o issue cuando aplique

La cantidad debe almacenarse como un valor positivo. El tipo del movimiento explica por que ocurrio; los buckets origen/destino determinan que cantidades se afectan.

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
