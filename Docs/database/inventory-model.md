# Inventory model

## Productos

`products` representa variantes vendibles.

Ejemplo:

```txt
product_details: Blusa floral 7000
products:
- talla M / rojo
- talla S / rojo
```

## Cantidades principales

```txt
quantity
received_quantity
available_quantity
reserved_quantity
unavailable_quantity
```

### quantity

Cantidad comprada en la orden.

### received_quantity

Cantidad fisicamente recibida.

### available_quantity

Cantidad disponible para vender.

### reserved_quantity

Cantidad comprometida por ventas reservadas.

No se maneja con `product_holds`. Si hay pago previo, debe existir una venta en estado `Reserved`.

### unavailable_quantity

Cantidad existente fisicamente, pero no vendible temporalmente.

Ejemplos:

- producto enviado para seleccion o prueba de talla (`product_holds`)
- producto danado
- producto sucio
- producto no encontrado fisicamente, pero pendiente de busqueda
- producto en revision o reparacion

Los productos enviados para seleccion se manejan con `product_holds`. Los issues operativos se manejan con `product_inventory_issues`.

## Invariante de stock

```txt
available_quantity + reserved_quantity + unavailable_quantity <= received_quantity
```

Las unidades vendidas, descartadas o perdidas definitivamente ya no forman parte de estas cantidades activas.

## Movimientos de inventario

Toda alteracion relevante debe crear un registro en `inventory_movements`.

Ejemplos:

- recepcion de compra
- venta
- devolucion
- dano
- perdida
- reparacion
- hallazgo
- descarte
- hold de seleccion
- liberacion de hold de seleccion
- conversion de hold de seleccion a venta

## Holds de seleccion

Cuando se crea un hold de seleccion:

```txt
available_quantity -= quantity
unavailable_quantity += quantity
```

Cuando se libera:

```txt
unavailable_quantity -= quantity
available_quantity += quantity
```

Cuando se convierte a venta:

```txt
unavailable_quantity -= quantity
```

No se vuelve a descontar `available_quantity`, porque ya fue descontado al crear el hold.

## Issues de inventario

Cuando un producto disponible deja de poder venderse temporalmente:

```txt
available_quantity -= quantity
unavailable_quantity += quantity
```

Se crea un `product_inventory_issue` con estado `Open` y un `inventory_movement` relacionado mediante `product_inventory_issue_id`.

Cuando el producto se repara o se encuentra:

```txt
unavailable_quantity -= quantity
available_quantity += quantity
```

Cuando el producto se descarta o se confirma perdido:

```txt
unavailable_quantity -= quantity
```

No se debe volver a descontar `available_quantity` si el producto ya habia salido de disponible al abrir el issue.

## Recepcion de productos

No se debe aumentar inventario disponible al crear una orden.

Se aumenta al recibir fisicamente productos.

```txt
received_quantity += cantidad_recibida
available_quantity += cantidad_recibida
```

## Productos danados, sucios o perdidos

No deben registrarse como ventas de monto cero.

Si la situacion es temporal o esta pendiente de resolucion, deben registrarse como `product_inventory_issues`.

Si la perdida o descarte ya es definitivo, debe quedar un `inventory_movement` de tipo `Lost` o `Discarded`.
