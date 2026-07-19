# Purchases Business Rules

## Objetivo

Registrar compras a proveedores, tracking numbers, costos de importación y recepción de productos sin obligar a dividir productos por caja o tracking.

## Tablas principales

* `orders`
* `order_statuses`
* `order_tracking_numbers`
* `suppliers`
* `shipping_companies`
* `products`
* `inventory_movements`

## Regla: una orden representa una compra al proveedor

Una orden agrupa productos comprados a un proveedor.

`orders` debe contener:

* proveedor
* fecha de orden
* estado
* moneda de compra
* monto total de mercadería en USD cuando aplique
* tasa de cambio utilizada
* monto total de mercadería en NIO
* costo de envío proveedor -> bodega en USD
* costo de envío bodega -> Nicaragua en USD cuando se conozca desde recepción
* costo total de la orden en NIO
* comentarios

La moneda de compra se guarda en `orders.purchase_currency_id`:

* `1 = USD`
* `2 = NIO`

El backend debe tomar la tasa desde la tabla de tasas de cambio (`DollarExchangeRates` en el código actual), filtrando por `Enabled = true` y usando `BankRate` del registro habilitado más reciente.

Si la compra es en USD, `orders.amount_usd` guarda el total comprado en dólares y `orders.merchandise_total_nio` se calcula multiplicando por la tasa.

Si la compra es local en NIO, `orders.merchandise_total_nio` guarda el total comprado en córdobas y `orders.amount_usd` guarda la equivalencia histórica dividiendo entre la tasa.

El frontend no debe enviar manualmente `orders.exchange_rate`.

El costo total de la orden se calcula así:

`TotalCostNio = MerchandiseTotalNio + SupplierShippingCostUsd × ExchangeRate + WarehouseShippingCostUsd × ExchangeRate`

Al crear o actualizar la orden, `WarehouseShippingCostUsd` queda en `0` porque el envío bodega -> Nicaragua se conoce posteriormente en recepción.

Los montos totales representan valores monetarios cerrados y deben almacenarse con dos decimales.

La tasa de cambio debe quedar guardada en la orden para conservar el valor histórico utilizado en la conversión, incluso en compras locales.

## Regla: un tracking representa logística, no detalle de productos

Los tracking numbers sirven para controlar paquetes o cajas recibidas, pero no se debe obligar a indicar qué productos venían en cada tracking.

Razón: operativamente se abren todas las cajas y luego se separa por producto.

Por tanto:

* `order_tracking_numbers` registra los paquetes.
* `products` registra lo comprado por producto, talla y color.
* La recepción real se registra por producto, no por tracking.

## Regla: una orden puede tener varios tracking numbers

Una orden puede tener uno o varios registros en `order_tracking_numbers`.

Cada tracking puede tener:

* compañía de envío
* número de tracking
* fecha de entrega
* peso
* costo de envío
* estado de entregado

Los costos registrados por tracking tienen fines logísticos. El costo de envío proveedor -> bodega se guarda en la orden en USD, se convierte a NIO con la tasa histórica de la orden y se distribuye entre sus líneas. El costo bodega -> Nicaragua se registra posteriormente desde recepción cuando se conozca.

## Regla: cálculo del costo de una línea de compra

Cada línea de producto debe conservar los siguientes valores:

* `unit_cost`
* `merchandise_total_cost_nio`
* `allocated_shipping_cost_nio`
* `total_cost_nio`
* `unit_cost_usd`
* `unit_cost_nio`

`unit_cost_nio` representa el costo unitario final en córdobas, incluyendo la parte correspondiente de los envíos registrados hasta el momento.

`unit_cost_usd` representa el equivalente histórico en dólares de `unit_cost_nio`, calculado con `orders.exchange_rate`. Por tanto, también incluye los envíos registrados hasta el momento.

`merchandise_total_cost_nio` representa el costo de mercadería asignado a la línea después de convertirla a córdobas cuando la compra es en USD, o el costo directo en córdobas cuando la compra es local.

Las diferencias de centavos producidas por el redondeo deben distribuirse entre las líneas para garantizar que:

`SUM(products.merchandise_total_cost_nio) = orders.merchandise_total_nio`

El costo de envío proveedor -> bodega, convertido a NIO, también debe distribuirse entre las líneas. Las diferencias de centavos deben ajustarse para garantizar que:

`SUM(products.allocated_shipping_cost_nio) = orders.supplier_shipping_cost_usd × orders.exchange_rate`

El costo total de la línea se calcula así:

`TotalCostNio = MerchandiseTotalCostNio + AllocatedShippingCostNio`

Los costos unitarios finales se calculan así:

`UnitCostNio = TotalCostNio / Quantity`

`UnitCostUsd = UnitCostNio / ExchangeRate`

`UnitCostNio` y `UnitCostUsd` ya incluyen la parte correspondiente de los envíos registrados y se actualizan cuando una recepción agrega costo bodega -> Nicaragua.

Los totales de línea deben almacenarse con dos decimales. `UnitCostUsd` se almacena con dos decimales. `UnitCostNio` debe almacenarse con mayor precisión, por ejemplo seis decimales, porque la división puede producir fracciones de centavo.


## Regla: recepción con costos bodega -> Nicaragua

La recepción de mercadería se realiza con:

`POST /api/v1/orders/{orderId}/receipts`

El mismo endpoint sirve para recepciones parciales y completas. El request indica qué productos y cantidades llegaron.

Si la orden tiene `order_tracking_numbers`, el peso y el costo USD de envío bodega -> Nicaragua se registran por tracking al momento de recibir. El backend suma esos costos para calcular el costo de recepción.

Si la orden no tiene tracking numbers, el request puede enviar `warehouseShippingCostUsd` directamente.

Al recibir productos, el backend debe:

* crear `product_receipts`
* crear `product_receipt_details`
* actualizar `order_tracking_numbers` cuando aplique
* aumentar `products.received_quantity`
* aumentar `products.available_quantity`
* convertir el costo bodega -> Nicaragua a NIO usando `orders.exchange_rate`
* acumular el costo en `orders.warehouse_shipping_cost_usd`
* sumar el costo convertido a `orders.total_cost_nio`
* actualizar `orders.received_amount_nio` con el valor de mercadería recibida en córdobas
* distribuir el costo convertido entre los productos recibidos usando el peso físico estimado enviado en el request
* actualizar `products.allocated_shipping_cost_nio`, `products.total_cost_nio`, `products.unit_cost_nio` y `products.unit_cost_usd`
* crear `inventory_movements` tipo `PurchaseReceived` con dirección `In`
* crear `financial_movements` tipo `Expense` con dirección `Out` cuando el costo sea mayor que cero
* actualizar la orden a `PartiallyReceived` o `Received`

`orders.received_amount_nio` refleja solamente mercadería recibida, sin incluir envíos. En una recepción completa debe ser igual a `orders.merchandise_total_nio`.

El costo bodega -> Nicaragua se paga y se conoce en la recepción, no al crear la orden.

El request de recepción puede enviar `products[].weight` para cada producto recibido. Este valor es un peso estimado por unidad definido por el negocio para distribuir el envío bodega -> Nicaragua. Por ejemplo: una blusa ligera puede usar `0.5`, una camisa normal `1`, un vestido ligero `2` o `3`, y un vestido largo o pesado `5` o `6`.

Si `products[].weight` no se envía, el backend usa `1`.

La base de distribución del envío es:

`products[].weight × products[].quantity`

Para registrar unidades recibidas de más, la línea debe enviar `products[].isSurplus = true` y un `products[].comments` que explique el sobrante. Sin esa marca explícita, la recepción sigue rechazando cantidades mayores a la cantidad pendiente.

## Regla: recepción parcial de compra

Si una orden llega en varias fechas, el sistema debe permitir recibir cantidades parciales por producto.

Campos relacionados:

* `products.quantity`
* `products.received_quantity`
* `products.available_quantity`
* `products.reserved_quantity`

Flujo:

1. Crear la orden con los productos comprados.
2. Inicializar `received_quantity = 0`.
3. Inicializar `available_quantity = 0`.
4. Cuando se recibe un producto, aumentar `received_quantity`.
5. Aumentar `available_quantity` en la misma cantidad.
6. Crear un `inventory_movement` de tipo `PurchaseReceived` relacionado con la orden y el producto.
7. No permitir que la recepción normal supere la cantidad pendiente.
8. Permitir que `received_quantity` supere `quantity` solo cuando la línea se marca explícitamente como sobrante.

Ejemplo:

* Comprado: 10 unidades.
* Recibido el primer día: 6 unidades.
* Recibido el segundo día: 4 unidades.

## Regla: las recepciones parciales pueden agregar costo de envío

Las recepciones parciales afectan las cantidades recibidas y disponibles. Si en esa recepción se registra costo bodega -> Nicaragua, ese costo se distribuye entre los productos recibidos y actualiza sus costos finales.

Al recibir parcialmente una línea:

* no se modifica `merchandise_total_cost_nio`
* sí puede aumentar `allocated_shipping_cost_nio` por el envío bodega -> Nicaragua
* sí puede aumentar `total_cost_nio`
* se recalculan `unit_cost_nio` y `unit_cost_usd`

El costo de envío proveedor -> bodega se distribuye al crear o actualizar la orden usando la cantidad total comprada de la línea. El costo bodega -> Nicaragua se distribuye en cada recepción usando el peso físico estimado enviado para los productos recibidos.

Si una línea recibe sobrantes, `quantity` sigue representando la cantidad comprada originalmente y `received_quantity` representa la cantidad física recibida. En ese caso, el costo unitario se calcula usando la mayor cantidad entre `quantity` y `received_quantity`, para no repartir el costo total entre menos unidades de las que realmente entraron al inventario.

Si el proveedor confirma que una parte de la línea nunca será entregada, la cantidad final de la línea y sus costos deben ajustarse antes de cerrar definitivamente la orden.

## Regla: faltantes confirmados y reembolso del proveedor

Cuando el proveedor confirme que una parte de una orden no llegará, el administrador debe cerrar los faltantes pendientes. Por cada variante pendiente se crea un `purchase_shortage` con la cantidad faltante, la fecha y el costo perdido congelado en `loss_amount_nio`.

Al cerrar faltantes con pérdida, la cantidad final de la variante queda igual a la cantidad realmente recibida y la orden pasa a `PendingRefund`: no admite más recepciones normales y queda pendiente de resolver el reembolso. El faltante no crea un nuevo egreso financiero: el pago original ya se registró como `SupplierPayment`.

Al registrar el único reembolso de proveedor o confirmar que no habrá reembolso, la orden pasa a `Received`. Si todos los faltantes tienen pérdida cero, no hay una acción financiera pendiente y la orden pasa directamente a `Received`.

Una orden puede tener un único `supplier_refund` opcional. Este guarda el monto total que el proveedor devolvió por todos sus faltantes, sin distribuirlo entre las variantes, y crea un movimiento financiero `SupplierRefund` de ingreso.

Si el proveedor confirma que no devolverá dinero, la orden guarda `supplier_refund_declined_at` y un comentario opcional. Esta resolución no crea un `supplier_refund` ni un movimiento financiero; tampoco permite registrar posteriormente un reembolso para la misma orden.

El estado de reembolso de cada línea se calcula, no se almacena:

* `PendingRefund`: no hay reembolso registrado.
* `PartiallyRefunded`: el reembolso total de la orden es mayor que cero y menor que la pérdida total.
* `Refunded`: el reembolso total es igual a la pérdida total.
* `NotRefunded`: el proveedor confirmó que no emitirá reembolso.

La pérdida neta de una compra es:

`SUM(purchase_shortages.loss_amount_nio) - supplier_refund.amount_nio`

## Regla: no vender productos no recibidos

El sistema solo debe vender desde `available_quantity`.

Si un producto fue comprado pero todavía no ha sido recibido, no debe estar disponible para la venta.

Antes de vender o reservar un producto, debe verificarse:

`available_quantity >= requested_quantity`

## Regla: costo histórico utilizado en una venta

Al crear una línea de venta, el sistema debe copiar:

`products.unit_cost_nio`

hacia:

`sale_details.unit_cost_at_sale`

Este valor debe quedar congelado en la venta.

Los cambios posteriores realizados en una orden o producto no deben modificar el costo histórico de una venta ya registrada.

La ganancia bruta de una línea se calcula así:

`GrossProfit = LineTotal - TotalCostAtSale`

Donde:

`TotalCostAtSale = UnitCostAtSale × Quantity`

## Regla: estado de orden

Estados sugeridos para `order_statuses`:

* `Pending`
* `PartiallyReceived`
* `Received`
* `Cancelled`
* `PendingRefund`

La orden debe pasar a `PartiallyReceived` cuando al menos una unidad haya sido recibida, pero todavía existan unidades pendientes.

La orden debe pasar a `Received` cuando todos sus productos hayan sido recibidos completamente, o después de resolver el reembolso de faltantes confirmados.

La orden debe pasar a `PendingRefund` cuando se cierran faltantes con pérdida: ya no hay unidades por recibir, pero falta registrar el reembolso único del proveedor o confirmar que no existirá.

Una orden cancelada no debe permitir nuevas recepciones.

## Regla: creación de producto detalle junto con la orden

En este negocio, la ropa cambia constantemente y rara vez se compra exactamente el mismo modelo muchas veces.

Por tanto, al crear una orden se deben registrar también los `product_details` comprados y sus variantes `products`.

`product_details` representa el modelo o artículo comprado:

* código del proveedor
* código interno del negocio
* nombre
* subcategoría

`products` representa las variantes vendibles de ese modelo:

* talla
* color
* cantidad comprada
* costo unitario en la moneda de compra
* precio de venta

El campo `product_details.code` es un entero y representa el código interno del negocio. Debe generarse en backend como un consecutivo que aumenta de 1 en 1.

Ejemplo:

```txt
product_details:
- code: 125
- supplier_product_code: SOHO25120
- name: Pantalón cargo

products:
- talla S / azul / cantidad 2
- talla M / azul / cantidad 3
- talla L / negro / cantidad 1
```

La API no debe pedir montos totales de la orden, tasa de cambio ni costos finales por variante. Esos valores se calculan a partir de la moneda de compra, la tasa bancaria habilitada cuando aplique y las variantes enviadas en el request.

## Regla: actualización de una orden de compra

Una orden puede actualizar sus datos generales y sus productos mientras no tenga inventario recibido, disponible, reservado ni recepciones registradas.

Si la orden ya tiene recepción física de productos, no se deben reemplazar sus líneas de compra desde la actualización de orden, porque eso puede alterar inventario y costos históricos.

Cuando una orden todavía no tiene inventario recibido, actualizar sus productos se trata como reemplazo completo de las líneas de compra (`products`), pero no debe quemar códigos internos de `product_details` si la operación es una corrección.

Para conservar `product_details.code`, el request de actualización debe enviar `productDetails[].id` para cada producto detalle existente. El backend debe reutilizar ese `product_detail`, actualizar sus datos editables y recrear sus variantes `products` con los nuevos costos, cantidades y precios.

Si el request incluye un `productDetails[].id` que no pertenece a la orden, la actualización debe rechazarse.

Si se agrega un producto detalle nuevo, se envía sin `id` y el backend asigna el siguiente `product_details.code` disponible.

Si un producto detalle existente no se incluye en la actualización, se considera eliminado de esa orden siempre que no tenga inventario recibido, disponible, reservado ni recepciones asociadas.

## Regla: movimiento financiero de compra

En el flujo actual, crear una orden de compra representa una salida real de dinero hacia el proveedor.

Por tanto, al crear una orden se debe crear también un `financial_movement` relacionado a `orders.id` con:

* `financial_movement_type_id = SupplierPayment`
* `movement_direction_id = Out`
* `amount = orders.total_cost_nio`
* `exchange_rate = orders.exchange_rate`
* `order_id = orders.id`

Si una orden se actualiza antes de tener inventario recibido o recepciones registradas, el movimiento financiero relacionado debe actualizarse para reflejar el nuevo total de la compra.

Una orden puede tener total financiero igual a cero mientras se registra de forma preliminar sin productos ni costos. En ese estado no debe crear ni conservar un `financial_movement` de compra. Cuando una actualización haga que `orders.total_cost_nio` sea mayor que cero, el backend debe crear o actualizar el movimiento financiero; si vuelve a cero antes de recibir inventario, debe eliminarlo.
