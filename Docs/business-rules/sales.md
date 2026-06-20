# Sales Business Rules

## Objetivo

Registrar ventas, líneas vendidas, estados, descuentos aplicados, costos históricos y totales sin mezclar pagos, entregas o movimientos de inventario en la misma tabla.

## Tablas principales

* `sales`
* `sale_details`
* `sale_statuses`
* `sale_detail_statuses`
* `sales_channels`
* `clients`
* `products`

## Regla: `sales` representa la venta general

`sales` debe contener datos de cabecera:

* fecha
* canal de venta
* cliente si aplica
* estado de venta
* subtotal antes de descuento
* descuento total
* subtotal final
* comentarios
* usuario que registró la venta

`sales` no debe representar pagos individuales ni entregas individuales.

## Regla: `sale_details` representa los productos vendidos

Cada fila de `sale_details` representa una línea de producto asociada a la venta.

Debe guardar:

* producto vendido
* cantidad
* precio unitario original
* descuento aplicado a la línea
* precio unitario final
* total de la línea
* costo unitario histórico
* costo total histórico
* ganancia bruta
* estado de la línea
* campaña de descuento si aplica
* fuente del descuento
* comentarios

## Regla: el costo del producto debe congelarse al vender

Cuando se registra una línea de venta, el sistema debe copiar:

`products.unit_cost_nio`

hacia:

`sale_details.unit_cost_at_sale`

`unit_cost_at_sale` representa el costo unitario histórico utilizado en la venta e incluye la parte correspondiente del envío de importación.

Este valor no debe recalcularse posteriormente aunque cambie el costo almacenado en `products`.

Esto permite que los reportes históricos conserven el costo que se conocía y utilizaba al momento de la venta.

## Regla: cálculo de una línea de venta

El total de una línea debe calcularse después de aplicar descuentos:

`LineTotal = FinalUnitPrice × Quantity`

El costo total histórico de la línea debe calcularse así:

`TotalCostAtSale = UnitCostAtSale × Quantity`

La ganancia bruta de la línea debe calcularse así:

`GrossProfit = LineTotal - TotalCostAtSale`

Ejemplo:

* Precio unitario final: C$300.00
* Cantidad: 2
* Costo unitario histórico: C$145.896000

Entonces:

`LineTotal = 300.00 × 2 = 600.00`

`TotalCostAtSale = 145.896000 × 2 = 291.792000`

`GrossProfit = 600.00 - 291.792000 = 308.208000`

La ganancia puede ser negativa. El sistema no debe impedir una venta únicamente porque su ganancia bruta sea menor que cero.

## Regla: precisión de costos y ganancias

Los precios y descuentos que representan montos cobrados deben almacenarse con dos decimales.

Los costos unitarios, costos totales calculados y ganancias deben conservar mayor precisión, por ejemplo seis decimales, para evitar diferencias acumuladas producidas por redondear cada línea prematuramente.

El sistema debe sumar las ganancias utilizando toda la precisión almacenada y redondear a dos decimales únicamente al presentar el total de un reporte.

No se debe redondear la ganancia de cada línea a dos decimales antes de calcular la ganancia total de un periodo.

## Regla: estados de venta

Estados sugeridos para `sale_statuses`:

* `Pending`
* `Confirmed`
* `PartiallyPaid`
* `Paid`
* `Delivered`
* `Cancelled`

La venta no debe eliminarse si se cancela. Debe cambiar de estado.

El estado de pago y el estado operativo de la venta podrían separarse en el futuro si se vuelve necesario, pero inicialmente pueden manejarse dentro del mismo catálogo.

## Regla: estados por línea de venta

Estados sugeridos para `sale_detail_statuses`:

* `Active`
* `Cancelled`
* `Refunded`
* `Exchanged`

Esto permite cancelar, devolver o cambiar un producto sin cancelar toda la venta.

Solo las líneas que correspondan según las reglas del reporte deben incluirse en los totales de unidades vendidas y ganancia.

Las líneas canceladas, reembolsadas o cambiadas deben conservarse como historial y no eliminarse físicamente.

## Regla: cancelación de una línea

Al cancelar una línea activa:

1. Cambiar su estado a `Cancelled`.
2. Revertir el inventario correspondiente.
3. Crear el movimiento de inventario correspondiente.
4. Ajustar los totales de la venta.
5. Revertir o registrar los movimientos financieros necesarios si ya se había recibido un pago.

No se debe eliminar la línea original.

## Regla: devolución de un producto

Cuando una clienta devuelve un producto:

1. Cambiar la línea original a estado `Refunded`.
2. Registrar el ingreso del producto al inventario si está en condiciones de volver a venderse.
3. Crear el movimiento de inventario correspondiente.
4. Registrar el reembolso financiero si aplica.
5. Conservar el costo histórico de la línea original.

Si el producto no puede volver a venderse, debe registrarse posteriormente como dañado o descartado mediante movimientos de inventario.

## Regla: venta con cambio de producto

Si una clienta cambia un producto por otra talla o producto:

1. La línea original debe cambiar a estado `Exchanged`.
2. La nueva línea debe agregarse en la misma venta con estado `Active`.
3. El producto original debe regresar al inventario si está en condiciones de venderse.
4. El producto nuevo debe salir del inventario como una venta.
5. Inventario y finanzas deben reflejar cualquier diferencia de precio.

No se debe crear una nueva venta automáticamente si el cambio forma parte de la misma transacción original.

La nueva línea debe copiar el costo actual del nuevo producto hacia `unit_cost_at_sale`.

## Regla: productos enviados para escoger talla

Si se mandan varias tallas para que la clienta escoja una:

* Solo debe aparecer como vendido el producto finalmente escogido.
* Las otras prendas deben manejarse mediante `product_holds`.
* Mientras estén reservadas, no deben estar disponibles para otras ventas.
* Al finalizar la selección, la prenda escogida se convierte en venta.
* Las prendas no seleccionadas regresan al inventario disponible.

Las prendas enviadas únicamente para selección no deben registrarse como líneas de venta activas.

## Regla: descuentos por línea

Cada línea debe conservar:

* precio unitario original
* descuento total asignado a la línea
* precio unitario final
* fuente del descuento
* campaña aplicada, si corresponde
* motivo del descuento manual, si corresponde

El precio final no debe recalcularse posteriormente usando la configuración actual de una campaña. Debe quedar congelado en la línea de venta.

## Regla: descuentos globales

Si se aplica un descuento general a toda la venta, el descuento debe distribuirse entre las líneas.

La suma de los descuentos asignados a las líneas debe coincidir exactamente con:

`sales.total_discount`

Las diferencias de centavos producidas durante la distribución deben ajustarse en una o varias líneas para garantizar que ambos valores cuadren.

La distribución permite calcular correctamente la ganancia de cada producto después del descuento.

## Regla: totales de venta

`sales.subtotal_before_discount` debe representar el total de productos antes de descuentos.

`sales.total_discount` debe representar la suma de todos los descuentos aplicados en las líneas.

`sales.subtotal` debe representar el total de productos después de descuentos.

Debe cumplirse:

`Subtotal = SubtotalBeforeDiscount - TotalDiscount`

También debe cumplirse:

`Subtotal = SUM(sale_details.line_total de las líneas correspondientes)`

El envío al cliente y los pagos no deben mezclarse directamente con `sale_details`.

## Regla: envío de importación y envío al cliente son conceptos distintos

El envío de importación forma parte de `products.unit_cost_nio` y, por tanto, del costo utilizado para calcular la ganancia del producto.

El envío al cliente pertenece a la entrega de una venta y debe registrarse por separado.

Si el negocio cobra una cantidad por envío y paga otra a la agencia, el resultado del envío se calcula así:

`ShippingProfit = ShippingCharged - ShippingCost`

Este resultado no debe modificar `sale_details.unit_cost_at_sale`.

## Regla: ganancia bruta de la venta

La ganancia bruta de productos de una venta puede obtenerse sumando:

`SUM(sale_details.gross_profit)`

La ganancia total de la operación puede considerar además:

`SaleOperationProfit = ProductGrossProfit + ShippingProfit - PaymentCommissions`

Esta cifra todavía no representa necesariamente la utilidad neta del negocio, porque no descuenta gastos operativos generales, pérdidas de inventario, pagos de préstamos u otros movimientos financieros.

## Regla: ventas y pagos son conceptos diferentes

Una venta puede tener cero, uno o varios pagos.

Por tanto, los pagos deben registrarse en `sale_payments`, no directamente como una única columna en `sales`.

La suma de los pagos puede ser:

* menor que el total de la venta, si está pendiente o parcialmente pagada
* igual al total de la venta, si está pagada
* mayor temporalmente solo si existe una regla explícita para saldo a favor o devolución

Cada pago debe generar el movimiento financiero correspondiente cuando represente una entrada real de dinero.
