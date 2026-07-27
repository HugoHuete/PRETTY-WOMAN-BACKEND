# Convertir product hold a venta

## Objetivo

Convertir en venta el producto que la clienta escogio despues de una seleccion, prueba o talla.

## Tablas involucradas

- `product_holds`
- `sales`
- `sale_products`
- `products`
- `inventory_movements`

## Flujo esperado

1. Buscar el `product_hold`.
2. Validar que este `Active`.
3. Crear o confirmar la venta y su `sale_product`.
4. Copiar el costo actual del producto a `sale_products.unit_cost_at_sale`.
5. Cambiar el hold a `ConvertedToSale`.
6. Colocar `resolved_at`.
7. Disminuir `products.unavailable_quantity`.
8. Crear `inventory_movements` relacionado con `product_hold_id` y/o `sale_product_id`.

## Reglas

- No volver a disminuir `available_quantity`; ya disminuyo cuando se creo el hold.
- Solo el producto escogido debe pasar a `sale_products`.
- Los productos no escogidos deben liberarse con estado `NotSelected`.

## Ejemplo

Antes del hold:

```txt
available_quantity += 1
```

Al crear hold de seleccion:

```txt
available_quantity -= 1
unavailable_quantity += 1
```

Al convertir a venta:

```txt
unavailable_quantity -= 1
```
