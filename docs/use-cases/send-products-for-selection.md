# Enviar productos para selección

## Objetivo

Enviar una o más prendas junto con una venta sin registrarlas todavía como vendidas. Una misma venta puede incluir productos ya confirmados y prendas enviadas para selección.

La selección pertenece exclusivamente a ventas destinadas a envío. Una venta con canal `InStoreSale` no puede crearse con prendas de selección, recibir holds de selección posteriormente ni cambiarse a ese canal si ya conserva historial de selección.

## Creación

La venta acepta dos listas independientes:

- `products`: productos confirmados que se agregan de inmediato a la venta.
- `selectionProducts`: prendas que se crean como `product_holds` con estado `Active` y razón `SentForSelection`.

`selectionProducts` debe estar vacío cuando `saleChannelId` sea `InStoreSale`. El envío puede crearse después de la venta; no es necesario que exista previamente para registrar la selección.

Cada prenda de selección disminuye `available_quantity` y aumenta `unavailable_quantity`. El sistema valida el stock total solicitado entre ambas listas.

## Resolución

Cada hold se resuelve individualmente:

- Si la clienta lo escoge, el hold pasa a `ConvertedToSale` y se agrega una línea a la misma venta.
- Si no lo escoge, pasa a `AwaitingReturn`: vuelve a ser comercialmente vendible, aunque se mantiene visible como pendiente de retorno físico.
- Cuando vuelve a tienda, pasa a `NotSelected`.

No se aplican mínimos ni máximos de selección: la clienta puede escoger todas, ninguna o cualquier combinación.

## Revertir una resolución

Admin puede reabrir una resolución con `POST /api/v1/sales/{id}/selection-holds/{holdId}/undo-resolution`. No requiere body y devuelve `204 No Content`. El hold vuelve a `Active` y la prenda vuelve a estar fuera de disponibilidad. Después de una respuesta exitosa, recargar el detalle de la venta.

La operación solo está disponible antes de que la venta se complete o se cancele. Puede revertir una prenda marcada como no seleccionada (`AwaitingReturn` o `NotSelected`) o una prenda convertida a venta (`ConvertedToSale`). Para la prenda convertida, el backend elimina su línea de la venta y recalcula los totales. La reversión se rechaza si esa línea tiene otros movimientos de inventario, si una devolución o un cambio la referencia, o si quitarla dejaría un total incompatible con los pagos registrados.

## Envío y conciliación

Un envío que tiene holds activos puede marcarse como `DeliveredPendingSelection`, pero no puede completarse ni conciliarse hasta que no exista ningún hold en estado `Active`.

Los holds en `ConvertedToSale`, `AwaitingReturn` y `NotSelected` se consideran resueltos para ese fin.
