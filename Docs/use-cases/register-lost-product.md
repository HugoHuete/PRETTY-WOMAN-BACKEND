# Registrar producto perdido

## Objetivo

Sacar de disponible un producto que no aparece fisicamente, sin registrarlo como venta.

Este flujo puede ser temporal si todavia se va a buscar el producto, o definitivo si ya se confirma la perdida.

## Cuando aplica

- Producto no aparece en conteo.
- Producto extraviado en tienda.
- Producto perdido durante manipulacion.
- Diferencia detectada en inventario.

## Tablas involucradas

- `products`
- `product_inventory_issues`
- `product_inventory_issue_types`
- `product_inventory_issue_statuses`
- `inventory_movements`
- `inventory_movement_types`

## Flujo esperado: perdida pendiente de busqueda

1. Buscar producto.
2. Validar que la cantidad sea mayor que cero.
3. Validar que `available_quantity >= quantity`.
4. Crear `product_inventory_issue` con tipo `Missing` y estado `Open`.
5. Disminuir `available_quantity`.
6. Aumentar `unavailable_quantity`.
7. Crear `inventory_movement` tipo `Lost`.
8. Relacionar el movimiento con `product_inventory_issue_id`.
9. Guardar comentario.

## Si se encuentra despues

1. Cambiar el issue a `ResolvedToAvailable`.
2. Disminuir `unavailable_quantity`.
3. Aumentar `available_quantity`.
4. Crear `inventory_movement` tipo `Found` relacionado al issue.

No se borra el movimiento original `Lost`.

## Si se confirma perdida definitiva

1. Cambiar el issue a `ConfirmedLost`.
2. Disminuir `unavailable_quantity`.
3. Crear un movimiento definitivo de perdida si se necesita para reportes contables.
4. No volver a disminuir `available_quantity`, porque ya se disminuyo al abrir el issue.

## Reglas de negocio

- No registrar como venta.
- Toda perdida debe tener movimiento de inventario.
- No se debe usar `product_holds` para productos perdidos.
- Si se encuentra despues, resolver el issue como encontrado; no registrar un ajuste manual separado.

## Errores esperados

- Stock insuficiente.
- Producto inexistente.
- Cantidad invalida.
- Issue inexistente al resolver.
- Issue ya cerrado.
