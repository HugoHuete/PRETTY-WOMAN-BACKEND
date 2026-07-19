# Registrar ajuste de inventario

## Objetivo

Corregir diferencias de inventario que no pertenecen a un flujo especifico como venta, devolucion, cambio, recepcion de compra, seleccion o issue de inventario.

## Cuando aplica

- Hubo cruce de codigo entre productos y se debe corregir la salida/entrada de ambos.
- Se dio de baja un producto por perdida y despues se encontro.
- Se detecto un faltante despues de haber recibido inventario.
- Se registra una salida no comercial como donacion.
- Se necesita una correccion manual auditada de conteo.

## Tablas involucradas

- `inventory_adjustments`
- `inventory_adjustment_items`
- `inventory_adjustment_reasons`
- `inventory_movements`
- `inventory_movement_types`
- `inventory_stock_buckets`
- `products`

## Flujo esperado

1. Validar que el motivo exista.
2. Validar que exista al menos un item.
3. Validar que cada item tenga producto, cantidad mayor que cero y buckets distintos.
4. Validar que los productos existan.
5. Crear `inventory_adjustment`.
6. Por cada item, ejecutar `InventoryService.Move`.
7. Crear `inventory_adjustment_item` y relacionarlo con su `inventory_movement`.
8. Guardar todo en la misma transaccion.

## Reglas de negocio

- Cada item del ajuste genera exactamente un movimiento de inventario.
- Los ajustes no modifican cantidades directamente; siempre pasan por `InventoryService`.
- Las transiciones deben estar permitidas por `InventoryService`.
- Un ajuste desde `External` aumenta `received_quantity` y no puede superar `products.quantity`.
- Si una diferencia pertenece a un flujo especifico, debe usarse ese flujo en vez de ajuste manual.
- `reference` debe usarse como folio o identificador corto para buscar el ajuste despues.
- `comments` debe usarse para explicar el contexto del ajuste en lenguaje humano.

## Campos de auditoria

`reference` es opcional y sirve para guardar una referencia corta y buscable del origen del ajuste.

Ejemplos:

- `CONTEO-JULIO-2026`
- `ORDEN-123`
- `FACTURA-9981`
- `VENTA-456`

`comments` es opcional y sirve para documentar la explicacion del ajuste.

Ejemplos:

- `Sobrante encontrado al recibir compra.`
- `Se corrigio cruce de codigo entre dos variantes.`
- `Conteo fisico mostro una unidad menos y no se encontro flujo asociado.`

## Ejemplos

Cruce de codigo en una venta:

- Producto incorrecto: `OutOfInventory -> Available`.
- Producto correcto: `Available -> OutOfInventory`.

Producto encontrado despues de baja:

- `OutOfInventory -> Available`.

Producto que deja de estar vendible por correccion manual:

- `Available -> Unavailable`.

Faltante detectado despues de recibir inventario:

- `Available -> OutOfInventory`.

## Errores esperados

- Motivo inexistente.
- Producto inexistente.
- Cantidad invalida.
- Buckets invalidos o iguales.
- Transicion no permitida.
- Stock insuficiente en el bucket origen.
- Entrada desde `External` que excede la cantidad comprada.
