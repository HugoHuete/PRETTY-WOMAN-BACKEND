# Convertir reserva en venta

## Objetivo

Marcar un producto reservado como vendido.

## Cuándo aplica

- La clienta recibió varias tallas y escogió una.
- Una reserva se confirma como compra.
- Un producto apartado pasa a formar parte de una venta.

## Tablas involucradas

- `product_holds`
- `products`
- `sales`
- `sale_details`
- `inventory_movements`

## Flujo esperado

1. Buscar reserva.
2. Validar que la reserva esté `Active`.
3. Buscar venta asociada o recibir `sale_id`.
4. Crear o actualizar `sale_details` con el producto vendido.
5. Cambiar `product_hold.status` a `ConvertedToSale`.
6. Guardar `resolved_at`.
7. Disminuir `products.reserved_quantity`.
8. Crear `inventory_movements` tipo `ReservationConvertedToSale`.

## Regla importante

No disminuir `available_quantity` en este paso, porque ya fue disminuido cuando se creó la reserva.

## Ejemplo

Al reservar:

```txt
available_quantity -= 1
reserved_quantity += 1
```

Al convertir en venta:

```txt
reserved_quantity -= 1
```

## Errores esperados

- Reserva inexistente.
- Reserva no activa.
- Venta inexistente.
- Producto ya convertido.
