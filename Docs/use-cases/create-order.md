# Crear orden de compra

## Objetivo

Registrar una compra al proveedor junto con los artículos comprados y sus variantes vendibles.

## Cuándo aplica

- Se hizo una compra nueva a un proveedor.
- La compra incluye uno o varios modelos de ropa.
- Cada modelo puede traer varias tallas y/o colores.
- La mercadería todavía no ha sido recibida físicamente.

## Tablas involucradas

- `orders`
- `product_details`
- `products`
- `suppliers`
- `subcategories`
- `sizes`
- `dollar_exchange_rates`

## Flujo esperado

1. Recibir proveedor, moneda de compra, costo de envío del proveedor a la bodega y productos comprados.
2. Crear la orden en estado `Pending`.
3. Crear un `product_detail` por cada modelo/artículo comprado.
4. Generar `product_details.code` en backend usando el siguiente consecutivo interno.
5. Crear un `product` por cada variante de talla/color.
6. Inicializar `received_quantity`, `available_quantity`, `reserved_quantity` y `unavailable_quantity` en `0`.
7. Obtener la tasa de cambio bancaria habilitada para conservar equivalencias históricas entre USD y NIO.
8. Calcular totales de orden a partir de las variantes.
9. Convertir el envío proveedor -> bodega desde USD a NIO y distribuirlo entre las variantes.
10. Guardar la orden y sus productos en la misma operación.

## Request esperado

```json
{
  "supplierId": 1,
  "purchaseCurrencyId": 1,
  "supplierShippingCostUsd": 15,
  "comments": "Compra SOHO junio",
  "productDetails": [
    {
      "supplierProductCode": "SOHO25120",
      "name": "Pantalon cargo",
      "subcategoryId": 5,
      "variants": [
        {
          "sizeId": 1,
          "color": "Azul",
          "quantity": 2,
          "unitCost": 8.5,
          "salePrice": 650
        },
        {
          "sizeId": 2,
          "color": "Azul",
          "quantity": 3,
          "unitCost": 8.5,
          "salePrice": 650
        }
      ]
    }
  ]
}
```

## Campos calculados

La API no debe recibir manualmente:

- `orders.amount_usd`
- `orders.exchange_rate`
- `orders.merchandise_total_nio`
- `orders.received_amount_nio`
- `orders.warehouse_shipping_cost_usd`
- `orders.total_cost_nio`
- `products.merchandise_total_cost_nio`
- `products.allocated_shipping_cost_nio`
- `products.total_cost_nio`
- `products.unit_cost_nio`
- cantidades recibidas, disponibles, reservadas o no disponibles

Estos valores se calculan en backend.

## Reglas de negocio

- Una orden puede crearse sin `productDetails` cuando la lista de productos todavía no está disponible.
- Si se envía un `product_detail`, debe traer al menos una variante `product`.
- No se permiten variantes duplicadas dentro del mismo `product_detail` para la misma talla y color.
- `purchaseCurrencyId = 1` representa compra en USD.
- `purchaseCurrencyId = 2` representa compra local en NIO.
- El frontend no envía `exchangeRate`; el backend la calcula.
- Para compras en USD, el backend usa `DollarExchangeRates.BankRate` del registro habilitado más reciente.
- Para compras en NIO, el backend usa `BankRate` para calcular la equivalencia histórica en USD.
- `unitCost` se interpreta en la moneda de compra de la orden; el producto conserva `unit_cost_usd` y `unit_cost_nio` calculados para reportes y costos históricos.
- `supplierShippingCostUsd` representa el envío proveedor -> bodega y siempre se envía en dólares.
- El backend convierte `supplierShippingCostUsd` a córdobas usando `orders.exchange_rate` y distribuye ese monto en `products.allocated_shipping_cost_nio`.
- `orders.warehouse_shipping_cost_usd` representa el envío bodega -> Nicaragua; se mantiene en `0` al crear/actualizar la orden y se debe completar desde el flujo de recepción cuando se conozca ese costo.
- `product_details.code` es un entero, representa el código interno del negocio y lo genera el backend.
- `product_details.supplier_product_code` es el código del proveedor.
- Al crear una orden no se envía `productDetails[].id`. `productDetails` puede omitirse o enviarse como arreglo vacío si los productos se agregarán después con `PUT /orders/{id}`.
- Al actualizar una orden, enviar `productDetails[].id` cuando se esté corrigiendo un `product_detail` existente para conservar su `code` interno.
- Si se agrega un `product_detail` nuevo durante la actualización, se envía sin `id` y el backend asigna el siguiente `code` disponible.
- El inventario disponible no aumenta al crear la orden.
- El inventario aumenta solamente al recibir productos.

## Errores esperados

- Proveedor inexistente.
- Subcategoría inexistente.
- Talla inexistente.
- Producto sin variantes.
- Variante duplicada para el mismo producto.
- Cantidad menor o igual a cero.
- Moneda de compra inválida.
- Compra en USD sin tasa bancaria habilitada.

## Movimiento financiero

Crear una orden también crea un movimiento financiero de egreso cuando `order.total_cost_nio` es mayor que cero:

```txt
type: SupplierPayment
direction: Out
amount: order.total_cost_nio
order_id: order.id
```

Si la orden se actualiza antes de recibir inventario, el movimiento financiero se crea, actualiza o elimina según el nuevo total. Si la orden queda sin productos ni costos, no debe conservar un movimiento financiero de monto cero.

## Actualizar productos de una orden

Cuando se actualiza una orden que todavía no tiene inventario recibido ni recepciones registradas, el request puede reemplazar sus variantes y corregir datos del modelo comprado.

Para conservar el código interno del negocio, cada `product_detail` existente debe enviarse con su `id`:

```json
{
  "id": 15,
  "supplierProductCode": "SOHO25120-CORREGIDO",
  "name": "Pantalon cargo corregido",
  "subcategoryId": 5,
  "variants": []
}
```

El backend conserva `product_details.code` para ese `id`. Solo los `product_details` nuevos, enviados sin `id`, reciben un código consecutivo nuevo.


