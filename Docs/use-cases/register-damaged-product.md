# Registrar producto dañado

## Objetivo

Dar de baja inventario por daño sin registrarlo como venta.

## Cuándo aplica

- Producto manchado.
- Producto roto.
- Producto defectuoso.
- Producto regresa de una venta pero no puede venderse nuevamente.

## Tablas involucradas

- `products`
- `inventory_movements`
- `inventory_movement_types`
- `financial_movements`, opcional

## Flujo esperado

1. Buscar producto.
2. Validar cantidad.
3. Disminuir `available_quantity` si estaba disponible.
4. Crear `inventory_movements` tipo `Damaged`.
5. Guardar comentario con el motivo.
6. Opcionalmente registrar pérdida financiera si quieres reflejar costo contable.

## Reglas de negocio

- No se debe registrar como venta de monto cero.
- El producto dañado debe afectar inventario.
- Debe quedar evidencia del motivo.
- Si estaba reservado, primero resolver la reserva.

## Errores esperados

- Producto inexistente.
- Cantidad inválida.
- Stock insuficiente.
