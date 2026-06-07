# Recibir productos de una orden

## Objetivo

Registrar cantidades recibidas físicamente de productos comprados.

## Cuándo aplica

- Llegó una orden completa.
- Llegó una orden parcialmente.
- Llegaron varios trackings pero se reciben productos mezclados.
- Se abre todo y luego se separa por producto.

## Tablas involucradas

- `orders`
- `products`
- `order_tracking_numbers`
- `product_receipts`, si se implementa
- `inventory_movements`

## Flujo esperado

1. Buscar orden.
2. Validar productos de la orden.
3. Registrar cantidades recibidas por producto.
4. Aumentar `products.received_quantity`.
5. Aumentar `products.available_quantity`.
6. Crear `inventory_movements` tipo `PurchaseReceived`.
7. Actualizar estado de la orden si quedó completa o parcial.
8. Actualizar tracking como recibido si corresponde.

## Reglas de negocio

- No es obligatorio separar productos por tracking.
- Los trackings sirven como control logístico.
- La recepción real se registra por producto.
- No se debe vender inventario no recibido.
- `received_quantity` no debe superar `quantity` comprada salvo que decidas permitir sobrantes.

## Errores esperados

- Orden inexistente.
- Producto no pertenece a la orden.
- Cantidad recibida inválida.
- Cantidad recibida excede cantidad comprada.
