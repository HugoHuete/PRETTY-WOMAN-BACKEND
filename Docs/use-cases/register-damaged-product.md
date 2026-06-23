# Registrar producto danado

## Objetivo

Sacar temporalmente de disponible un producto que no puede venderse por dano, sin registrarlo como venta.

## Cuando aplica

- Producto manchado.
- Producto roto.
- Producto defectuoso.
- Producto regresa de una venta pero no puede venderse nuevamente todavia.
- Producto necesita revision o reparacion antes de decidir si vuelve a venta o se descarta.

## Tablas involucradas

- `products`
- `product_inventory_issues`
- `product_inventory_issue_types`
- `product_inventory_issue_statuses`
- `inventory_movements`
- `inventory_movement_types`
- `financial_movements`, opcional para contabilidad futura, no para caja

## Flujo esperado

1. Buscar producto.
2. Validar que la cantidad sea mayor que cero.
3. Validar que `available_quantity >= quantity`.
4. Crear `product_inventory_issue` con tipo `Damaged` y estado `Open`.
5. Disminuir `products.available_quantity`.
6. Aumentar `products.unavailable_quantity`.
7. Crear `inventory_movement` tipo `Damaged`.
8. Relacionar el movimiento con `product_inventory_issue_id`.
9. Guardar comentario con el motivo.

## Resolucion esperada

Si el producto se repara y vuelve a venderse:

1. Cambiar el issue a `ResolvedToAvailable`.
2. Disminuir `unavailable_quantity`.
3. Aumentar `available_quantity`.
4. Crear `inventory_movement` tipo `Repaired` relacionado al issue.

Si el producto no puede recuperarse:

1. Cambiar el issue a `Discarded`.
2. Disminuir `unavailable_quantity`.
3. Crear `inventory_movement` tipo `Discarded` relacionado al issue.

## Reglas de negocio

- No se debe registrar como venta de monto cero.
- No se debe usar `product_holds` para productos danados.
- El producto danado debe afectar inventario moviendo unidades de disponible a no disponible.
- Debe quedar evidencia del motivo.
- Si estaba reservado, primero resolver la reserva comercial.

## Errores esperados

- Producto inexistente.
- Cantidad invalida.
- Stock insuficiente.
- Issue inexistente al resolver.
- Issue ya cerrado.
