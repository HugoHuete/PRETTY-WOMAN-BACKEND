# Returns and Exchanges Business Rules

## Objetivo

Manejar cancelaciones, reembolsos y cambios de productos sin inflar ventas ni perder trazabilidad de inventario y dinero.

## Tablas principales

- `sales`
- `sale_details`
- `sale_returns` y `sale_return_items`
- `sale_exchanges`, `exchange_return_items` y `exchange_outbound_items`
- `inventory_movements`
- `financial_movements`
- `sale_payments`
- `products`

## Regla: no borrar ventas ni líneas

Si una venta se cancela, no debe eliminarse: cambia a `Cancelled`. Las líneas de venta permanecen como el registro histórico de la operación.

Las devoluciones y cambios se registran en sus propios agregados y no cambian el estado de la línea original.

## Regla: cancelación total de venta

Cuando se cancela una venta completa:

1. Cambiar `sales.sale_status_id` a `Cancelled`.
2. Revertir inventario de líneas que ya habían descontado stock.
3. Crear movimientos de inventario de reversión.
4. Si hubo pagos, exigir que se reembolsen antes de completar la cancelación.

## Regla: corrección parcial posterior a la venta

El sistema no cancela líneas de venta individualmente. Una devolución parcial usa `SaleReturn` y un cambio usa `SaleExchange`; ambos conservan la línea original y registran cantidades, inventario y dinero por separado.

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

1. Registrar el retorno en `ExchangeReturnItem` y la prenda de salida en `ExchangeOutboundItem`.
2. Reservar la prenda de salida y registrar los movimientos de inventario al entregarla y recibir el retorno.
3. Mantener la venta y su línea original inmutables.
4. Si no hay diferencia de precio, no crear movimiento financiero.

## Regla: cambio por otro producto

Si una clienta cambia por un producto diferente:

1. Crear un `SaleExchange` con sus ítems de retorno y salida.
2. Ajustar el inventario de ambos productos mediante los movimientos del cambio.
3. Si el nuevo producto cuesta más, registrar pago adicional.
4. Si el nuevo producto cuesta menos, registrar devolución o saldo según la política del negocio.

No se debe crear una nueva venta si el cambio es parte de la misma operación de postventa.

## Regla: venta nueva vs cambio

Crear nueva venta solo si la clienta realiza una compra nueva independiente.

Mantener en la misma venta si:

- es cambio de talla
- es cambio por producto similar
- es ajuste de una venta reciente
- se desea conservar trazabilidad de la venta original

## Regla: ganancia en cambios

Los ítems del cambio conservan sus precios y costos históricos. La trazabilidad entre la línea original y el reemplazo se mantiene en los ítems de `SaleExchange`.
