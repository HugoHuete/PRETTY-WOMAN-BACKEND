# Returns and Exchanges Business Rules

## Objetivo

Manejar cancelaciones, reembolsos y cambios de productos sin inflar ventas ni perder trazabilidad de inventario y dinero.

## Tablas principales

- `sales`
- `sale_details`
- `sale_detail_statuses`
- `inventory_movements`
- `financial_movements`
- `sale_payments`
- `products`

## Regla: no borrar ventas ni líneas

Si una venta o línea se cancela, devuelve o cambia, no debe eliminarse.

Debe cambiar de estado.

## Regla: cancelación total de venta

Cuando se cancela una venta completa:

1. Cambiar `sales.sale_status_id` a `Cancelled`.
2. Cambiar líneas activas a `Cancelled`.
3. Revertir inventario de líneas que ya habían descontado stock.
4. Crear movimientos de inventario de reversión.
5. Si ya hubo pago, registrar devolución o movimiento financiero de egreso.

## Regla: cancelación de producto específico

Si se cancela solo un producto dentro de una venta:

1. Cambiar `sale_details.sale_detail_status_id` a `Cancelled`.
2. Regresar inventario si ya había sido descontado.
3. Recalcular totales de la venta si aplica.
4. Registrar movimiento financiero si hay devolución de dinero.

## Regla: devolución posterior a una venta

Una devolución es distinta de una cancelación y de un cambio: usa `SaleReturn` y `SaleReturnItem`, y no modifica la venta ni sus pagos históricos. Cada ítem conserva `OriginalUnitCost` para calcular la utilidad histórica.

- Solo admite prendas cuya venta ya descontó inventario; la suma devuelta o cambiada no puede superar lo vendido.
- Por agencia se reembolsa al retiro; en local al recibir la prenda.
- Una prenda buena vuelve a disponible. Una dañada queda no disponible con un issue `Damaged` abierto.
- Por preferencia, el envío de retorno puede descontarse del reembolso y no se devuelve el envío original.
- Por defecto, error de tienda, la tienda asume el retorno; el envío original solo se devuelve si la venta tenía una sola prenda.
- El costo del retorno de agencia se paga en su conciliación, no desde los pagos de la venta.

Ver `Docs/use-cases/return-products-after-sale.md`.

## Regla: cambio por otra talla

Si una clienta cambia talla:

1. Cambiar línea original a `Exchanged`.
2. Crear nueva línea en la misma venta con la nueva talla.
3. Regresar inventario de la talla original si vuelve disponible.
4. Descontar inventario de la nueva talla.
5. Registrar movimientos de inventario.
6. Si no hay diferencia de precio, no crear movimiento financiero.

## Regla: cambio por otro producto

Si una clienta cambia por un producto diferente:

1. Cambiar línea original a `Exchanged`.
2. Crear nueva línea en la misma venta.
3. Ajustar inventario de ambos productos.
4. Si el nuevo producto cuesta más, registrar pago adicional.
5. Si el nuevo producto cuesta menos, registrar devolución o saldo según la política del negocio.

No se debe crear una nueva venta si el cambio es parte de la misma operación de postventa.

## Regla: venta nueva vs cambio

Crear nueva venta solo si la clienta realiza una compra nueva independiente.

Mantener en la misma venta si:

- es cambio de talla
- es cambio por producto similar
- es ajuste de una venta reciente
- se desea conservar trazabilidad de la venta original

## Regla: ganancia en cambios

Al cambiar líneas, la ganancia debe recalcularse o registrarse en las líneas afectadas según el precio final y costo histórico de cada producto.

La venta debe conservar trazabilidad de qué producto fue reemplazado mediante estado, comentarios o campo adicional si se implementa.
