# Sales Business Rules

## Objetivo

Registrar ventas, líneas vendidas, estados, descuentos aplicados y totales sin mezclar pagos, entregas o movimientos de inventario en la misma tabla.

## Tablas principales

- `sales`
- `sale_details`
- `sale_statuses`
- `sale_detail_statuses`
- `sales_channels`
- `clients`
- `products`

## Regla: `sales` representa la venta general

`sales` debe contener datos de cabecera:

- fecha
- canal de venta
- cliente si aplica
- estado de venta
- subtotal antes de descuento
- descuento total
- subtotal final
- comentarios
- usuario que registró la venta

`sales` no debe representar pagos individuales ni entregas individuales.

## Regla: `sale_details` representa los productos vendidos

Cada fila de `sale_details` representa una línea de producto asociada a la venta.

Debe guardar:

- producto vendido
- cantidad
- precio original
- descuento aplicado a la línea
- precio final
- estado de la línea
- campaña de descuento si aplica
- fuente del descuento
- comentarios

## Regla: estados de venta

Estados sugeridos para `sale_statuses`:

- `Pending`
- `Confirmed`
- `PartiallyPaid`
- `Paid`
- `Delivered`
- `Cancelled`

La venta no debe eliminarse si se cancela. Debe cambiar de estado.

## Regla: estados por línea de venta

Estados sugeridos para `sale_detail_statuses`:

- `Active`
- `Cancelled`
- `Refunded`
- `Exchanged`

Esto permite cancelar, devolver o cambiar un producto sin cancelar toda la venta.

## Regla: venta con cambio de producto

Si una clienta cambia un producto por otra talla o producto:

1. La línea original debe cambiar a estado `Exchanged`.
2. La nueva línea debe agregarse en la misma venta con estado `Active`.
3. Inventario y finanzas deben reflejar la diferencia si existe.

No se debe crear una nueva venta automáticamente si el cambio forma parte de la misma transacción original.

## Regla: productos enviados para escoger talla

Si se mandan varias tallas para que la clienta escoja una:

- Solo debe aparecer como vendido el producto finalmente escogido.
- Las otras prendas deben manejarse como reservas o movimientos de inventario, no como ventas.

## Regla: totales de venta

`sales.subtotal_before_discount` debe representar el total de productos antes de descuento.

`sales.total_discount` debe representar la suma de todos los descuentos aplicados en las líneas.

`sales.subtotal` debe representar el total de productos después de descuentos.

El envío y los pagos no deben mezclarse directamente con `sale_details`.

## Regla: ventas y pagos son conceptos diferentes

Una venta puede tener cero, uno o varios pagos.

Por tanto, los pagos deben registrarse en `sale_payments`, no directamente como una columna única en `sales`.
