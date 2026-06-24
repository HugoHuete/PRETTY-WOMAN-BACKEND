# Product Inventory Issues Business Rules

## Objetivo

Registrar unidades que existen fisicamente, pero que no pueden venderse temporalmente por una condicion operativa.

Ejemplos:

- producto danado
- producto sucio
- producto no encontrado fisicamente, pero pendiente de busqueda
- producto en revision
- producto en reparacion

Este modulo no representa productos enviados para seleccion de talla. Esos casos viven en `product_holds`.

## Tablas principales

- `product_inventory_issues`
- `product_inventory_issue_types`
- `product_inventory_issue_statuses`
- `products`
- `inventory_movements`
- `inventory_movement_types`

## Responsabilidad de `product_inventory_issues`

`product_inventory_issues` representa el caso operativo abierto o historico de una unidad no disponible para venta.

Sirve para responder:

- Que productos estan temporalmente no disponibles?
- Por que no estan disponibles?
- Cuantas unidades estan afectadas?
- Cuando se abrio el caso?
- Como se resolvio?
- Que movimientos de inventario respaldan la entrada y salida del caso?

## Cantidades involucradas

`products.available_quantity` representa unidades vendibles.

`products.reserved_quantity` representa unidades comprometidas por ventas reservadas.

`products.unavailable_quantity` representa unidades existentes que no se pueden vender temporalmente, incluyendo issues operativos y holds de seleccion.

Debe cumplirse:

```txt
available_quantity + reserved_quantity + unavailable_quantity <= received_quantity
```

## Tipos de issue

Catalogo `product_inventory_issue_types`:

```txt
Damaged
Dirty
Missing
UnderReview
Repairing
```

## Estados de issue

Catalogo `product_inventory_issue_statuses`:

```txt
Open
ResolvedToAvailable
Discarded
ConfirmedLost
Cancelled
```

## Regla: abrir issue desde inventario disponible

Cuando una unidad disponible deja de poder venderse temporalmente:

1. Validar que `available_quantity >= quantity`.
2. Crear `product_inventory_issue` con estado `Open`.
3. Disminuir `products.available_quantity`.
4. Aumentar `products.unavailable_quantity`.
5. Crear `inventory_movement` con tipo especifico, por ejemplo `Damaged` o `Lost`.
6. Relacionar el movimiento con `product_inventory_issue_id`.
7. Guardar comentario obligatorio cuando la causa no sea evidente.

Cambios:

```txt
available_quantity -= quantity
unavailable_quantity += quantity
```

## Regla: resolver issue y devolver a disponible

Cuando la unidad se encontro, se limpio o se reparo y puede volver a venderse:

1. Validar que el issue este `Open`.
2. Cambiar el estado a `ResolvedToAvailable`.
3. Colocar `resolved_at`.
4. Disminuir `products.unavailable_quantity`.
5. Aumentar `products.available_quantity`.
6. Crear `inventory_movement` de tipo `Found` o `Repaired`, segun corresponda.
7. Relacionar el movimiento con `product_inventory_issue_id`.

Cambios:

```txt
unavailable_quantity -= quantity
available_quantity += quantity
```

## Regla: descartar issue

Cuando se confirma que la unidad no podra recuperarse ni venderse:

1. Validar que el issue este `Open`.
2. Cambiar el estado a `Discarded` o `ConfirmedLost`.
3. Colocar `resolved_at`.
4. Disminuir `products.unavailable_quantity`.
5. Crear `inventory_movement` de tipo `Discarded` o `Lost`.
6. Relacionar el movimiento con `product_inventory_issue_id`.
7. Conservar el costo historico del producto en el movimiento cuando se use para reportes de perdida.

Cambios:

```txt
unavailable_quantity -= quantity
```

No se debe disminuir `available_quantity` en este paso si la unidad ya habia salido de disponible al abrir el issue.

## Regla: cancelar issue

Un issue solo debe cancelarse si fue creado por error.

1. Validar que el issue este `Open`.
2. Cambiar estado a `Cancelled`.
3. Colocar `resolved_at`.
4. Disminuir `products.unavailable_quantity`.
5. Aumentar `products.available_quantity`.
6. Crear un movimiento que deje evidencia, normalmente `AdjustmentIncrease` o un tipo especifico si existe.

Si el producto realmente estuvo no disponible y luego se recupero, usar `ResolvedToAvailable`, no `Cancelled`.

## Relacion con reservas

No usar `product_inventory_issues` para apartar productos para clientas.

No usar `product_holds` para productos danados, sucios, perdidos o en revision.

Si un producto reservado se dana o se pierde, primero debe resolverse la reserva comercial segun el flujo de negocio, y luego abrir el issue operativo correspondiente.

## Reglas de consistencia

- Todo cambio de `available_quantity`, `reserved_quantity` o `unavailable_quantity` debe generar un `inventory_movement`.
- Todo movimiento asociado a un issue debe guardar `product_inventory_issue_id`.
- Un issue cerrado no debe volver a modificarse.
- Los movimientos historicos no deben eliminarse.
- Las cantidades deben cambiarse en la misma transaccion que crea el movimiento.
