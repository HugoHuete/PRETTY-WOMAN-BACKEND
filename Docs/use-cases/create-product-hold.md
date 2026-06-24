# Crear product hold

## Objetivo

Registrar productos enviados temporalmente para seleccion, prueba o talla sin marcarlos como vendidos.

No aplica para reservas con pago previo; esas deben crearse como ventas en estado `Reserved`.

## Tablas involucradas

- `product_holds`
- `products`
- `inventory_movements`

## Flujo esperado

1. Validar que el producto exista.
2. Validar que `available_quantity >= quantity`.
3. Crear `product_hold` con estado `Active` y razon `SentForSelection`.
4. Disminuir `products.available_quantity`.
5. Aumentar `products.unavailable_quantity`.
6. Crear `inventory_movements` relacionado con `product_hold_id`.

## Reglas

- El producto en hold de seleccion no esta vendido.
- El producto en hold de seleccion no debe estar disponible para otras ventas.
- El hold debe cerrarse como `ConvertedToSale` si la clienta escoge el producto.
- El hold debe cerrarse como `NotSelected` si el producto regresa disponible.

## Ejemplo

Antes:

```txt
available_quantity = 2
unavailable_quantity = 0
```

Crear hold de seleccion por 1:

```txt
available_quantity = 1
unavailable_quantity = 1
```
