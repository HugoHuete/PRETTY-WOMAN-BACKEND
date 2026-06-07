# Registrar producto perdido

## Objetivo

Dar de baja inventario por pérdida.

## Cuándo aplica

- Producto no aparece en conteo.
- Producto extraviado en tienda.
- Producto perdido durante manipulación.
- Diferencia detectada en inventario.

## Tablas involucradas

- `products`
- `inventory_movements`
- `inventory_movement_types`

## Flujo esperado

1. Buscar producto.
2. Validar cantidad perdida.
3. Disminuir `available_quantity`.
4. Crear `inventory_movements` tipo `Lost`.
5. Guardar comentario.

## Reglas de negocio

- No registrar como venta.
- Toda pérdida debe tener movimiento de inventario.
- Si se encuentra después, se registra movimiento tipo `Adjustment` o `Found`.

## Errores esperados

- Stock insuficiente.
- Producto inexistente.
- Cantidad inválida.
