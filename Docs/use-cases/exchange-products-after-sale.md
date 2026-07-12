# Cambiar productos después de una venta

Un cambio no modifica los productos ni los pagos históricos de la venta original. Se registra como un `SaleExchange` vinculado a ella.

## Crear el cambio

`POST /api/v1/sales/{saleId}/exchanges` recibe las prendas retornadas, identificadas por su `originalSaleProductId`, y las prendas de salida. Estas últimas se clasifican como `Replacement` o `AdditionalPurchase`.

Al crearlo, las prendas de salida se mueven de disponible a reservado. El saldo del cambio es el total de salida menos el crédito reconocido a la clienta.

### Crédito reconocido por prenda retornada

`recognizedUnitAmount` es el crédito acordado **por cada unidad** que la clienta devuelve. Se descuenta del total de las prendas de salida para calcular `balanceToCollect`. En un cambio normal debe ser igual al precio final que la clienta pagó por esa prenda; se puede indicar un valor menor solamente cuando exista una excepción acordada (por ejemplo, una política de cambio o una condición especial de la prenda). Nunca puede ser negativo ni mayor al precio final cobrado originalmente.

## Retorno por agencia

El intercambio físico se registra una sola vez con `handover`: la agencia entrega las prendas nuevas y recibe las prendas originales en el mismo acto. Cada prenda original vuelve de `OutOfInventory` a `Available` y queda identificada por el item de retorno como pendiente de retorno físico. Al llegar a tienda se registra `received`; si no llega, se registra `missing` y se revierte su disponibilidad.

## Entrega y ganancia

La entrega de las prendas de salida convierte su reserva en salida de inventario. La ganancia neta del cambio combina la ganancia de esas prendas con la reversión económica de cada prenda retornada que la agencia aceptó. Los `SaleProducts` originales no se editan, por lo que se preserva la trazabilidad de la venta.

Este primer flujo registra el saldo del cambio y sus movimientos de inventario. La creación y conciliación de envíos propios del cambio y la asignación de un mismo cobro entre saldo de venta y saldo de cambio requieren extender el modelo de pagos y envíos, que actualmente pertenece exclusivamente a `Sale`.
