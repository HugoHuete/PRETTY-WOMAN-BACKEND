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
* monto total de mercadería en USD
* tasa de cambio utilizada
* monto total de mercadería en NIO
* costo total de envío de importación en NIO
* costo total de la orden en NIO
* comentarios

El costo total de la orden se calcula así:

`TotalCostNio = MerchandiseTotalNio + ShippingCostNio`

Los montos totales representan valores monetarios cerrados y deben almacenarse con dos decimales.

La tasa de cambio debe quedar guardada en la orden para conservar el valor histórico utilizado en la conversión.

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

Los costos registrados por tracking tienen fines logísticos. El costo total de envío de importación utilizado para calcular el costo de los productos se guarda en la orden y se distribuye entre sus líneas.

## Regla: cálculo del costo de una línea de compra

Cada línea de producto debe conservar los siguientes valores:

* `unit_cost_usd`
* `merchandise_total_cost_nio`
* `allocated_shipping_cost_nio`
* `total_cost_nio`
* `unit_cost_nio`

`merchandise_total_cost_nio` representa el costo de mercadería asignado a la línea después de convertirla a córdobas.

Las diferencias de centavos producidas por el redondeo deben distribuirse entre las líneas para garantizar que:

`SUM(products.merchandise_total_cost_nio) = orders.merchandise_total_nio`

El costo de envío de importación también debe distribuirse entre las líneas. Las diferencias de centavos deben ajustarse para garantizar que:

`SUM(products.allocated_shipping_cost_nio) = orders.shipping_cost_nio`

El costo total de la línea se calcula así:

`TotalCostNio = MerchandiseTotalCostNio + AllocatedShippingCostNio`

El costo unitario final se calcula así:

`UnitCostNio = TotalCostNio / Quantity`

`UnitCostNio` ya incluye la parte correspondiente del costo de envío de importación y es el valor utilizado posteriormente para calcular la ganancia de una venta.

Los totales de línea deben almacenarse con dos decimales. `UnitCostNio` debe almacenarse con mayor precisión, por ejemplo seis decimales, porque la división puede producir fracciones de centavo.

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
7. No permitir que `received_quantity` supere `quantity`.

Ejemplo:

* Comprado: 10 unidades.
* Recibido el primer día: 6 unidades.
* Recibido el segundo día: 4 unidades.

## Regla: las recepciones parciales no modifican el costo

Las recepciones parciales afectan las cantidades recibidas y disponibles, pero no generan lotes de costo diferentes.

Al recibir parcialmente una línea:

* no se modifica `merchandise_total_cost_nio`
* no se modifica `allocated_shipping_cost_nio`
* no se modifica `total_cost_nio`
* no se modifica `unit_cost_nio`

El costo de envío de importación se distribuye usando la cantidad total comprada de la línea, aunque las unidades sean recibidas en momentos distintos.

Esta simplificación se adopta porque las recepciones parciales son poco frecuentes y, normalmente, todas las unidades de un mismo producto se reciben juntas.

Si el proveedor confirma que una parte de la línea nunca será entregada, la cantidad final de la línea y sus costos deben ajustarse antes de cerrar definitivamente la orden.

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

La orden debe pasar a `PartiallyReceived` cuando al menos una unidad haya sido recibida, pero todavía existan unidades pendientes.

La orden debe pasar a `Received` solamente cuando todos sus productos hayan sido recibidos completamente.

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
* costo unitario en USD
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

La API no debe pedir montos totales de la orden ni costos finales por variante. Esos valores se calculan a partir de las variantes enviadas en el request.

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

Una compra no puede tener total financiero menor o igual a cero, porque `financial_movements` representa movimientos reales de dinero.

