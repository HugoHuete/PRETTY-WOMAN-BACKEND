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

## Flujo esperado

1. Recibir proveedor, tasa de cambio, costo total de envío de importación y productos comprados.
2. Crear la orden en estado `Pending`.
3. Crear un `product_detail` por cada modelo/artículo comprado.
4. Generar `product_details.code` en backend usando el siguiente consecutivo interno.
5. Crear un `product` por cada variante de talla/color.
6. Inicializar `received_quantity`, `available_quantity` y `reserved_quantity` en `0`.
7. Calcular totales de orden a partir de las variantes.
8. Distribuir costo de mercadería y envío entre las variantes.
9. Guardar la orden y sus productos en la misma operación.

## Request esperado

```json
{
  "supplierId": 1,
  "exchangeRate": 36.75,
  "shippingCostNio": 500,
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
          "unitCostUsd": 8.5,
          "salePrice": 650
        },
        {
          "sizeId": 2,
          "color": "Azul",
          "quantity": 3,
          "unitCostUsd": 8.5,
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
- `orders.merchandise_total_nio`
- `orders.received_amount_nio`
- `orders.total_cost_nio`
- `products.merchandise_total_cost_nio`
- `products.allocated_shipping_cost_nio`
- `products.total_cost_nio`
- `products.unit_cost_nio`
- cantidades recibidas, disponibles o reservadas

Estos valores se calculan en backend.

## Reglas de negocio

- Cada orden debe traer al menos un `product_detail`.
- Cada `product_detail` debe traer al menos una variante `product`.
- No se permiten variantes duplicadas dentro del mismo `product_detail` para la misma talla y color.
- `product_details.code` es un entero, representa el código interno del negocio y lo genera el backend.
- `product_details.supplier_product_code` es el código del proveedor.
- Al crear una orden no se envía `productDetails[].id`.
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
- Tasa de cambio menor o igual a cero.

## Movimiento financiero

Crear una orden también crea un movimiento financiero de egreso:

```txt
type: SupplierPayment
direction: Out
amount: order.total_cost_nio
order_id: order.id
```

Si la orden se actualiza antes de recibir inventario, el movimiento financiero se actualiza con el nuevo total.

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


