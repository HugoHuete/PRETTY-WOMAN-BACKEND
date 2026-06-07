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
```

### quantity

Cantidad comprada en la orden.

### received_quantity

Cantidad físicamente recibida.

### available_quantity

Cantidad disponible para vender.

### reserved_quantity

Cantidad apartada o fuera de tienda, pero no vendida.

## Movimientos de inventario

Toda alteración relevante debe crear un registro en `inventory_movements`.

Ejemplos:

- recepción de compra
- venta
- devolución
- daño
- pérdida
- reserva
- liberación de reserva
- conversión de reserva a venta

## Reservas

Cuando se reserva un producto:

```txt
available_quantity -= quantity
reserved_quantity += quantity
```

Cuando se libera:

```txt
reserved_quantity -= quantity
available_quantity += quantity
```

Cuando se convierte a venta:

```txt
reserved_quantity -= quantity
```

No se vuelve a descontar `available_quantity`, porque ya fue descontado al crear la reserva.

## Recepción de productos

No se debe aumentar inventario disponible al crear una orden.

Se aumenta al recibir físicamente productos.

```txt
received_quantity += cantidad_recibida
available_quantity += cantidad_recibida
```

## Productos dañados o perdidos

No deben registrarse como ventas de monto cero.

Deben registrarse como movimientos de inventario:

```txt
Damaged
Lost
```

y disminuir stock disponible.
