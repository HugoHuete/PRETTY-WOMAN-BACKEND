# Responsabilidad de tablas

## suppliers

Catálogo de proveedores.

Guarda datos básicos del proveedor al que se compran productos.

## shipping_companies

Catálogo de empresas usadas para envíos de compras/proveedores, si aplica.

## order_statuses

Catálogo de estados para órdenes de compra.

Ejemplos:

- Pending
- Ordered
- PartiallyReceived
- Received
- Cancelled

## orders

Representa una compra realizada a un proveedor.

No representa inventario disponible por sí sola.  
El inventario debe aumentar cuando se reciben productos.

## order_tracking_numbers

Representa guías o tracking numbers asociados a una orden.

Sirve como control logístico.  
No necesariamente define qué productos venían en cada tracking.

## categories

Categorías generales de productos.

Ejemplos:

- Ropa
- Accesorios

## subcategories

Subcategorías dentro de una categoría.

Ejemplos:

- Vestidos
- Blusas
- Pantalones

## sizes

Catálogo de tallas.

Sirve para evitar valores inconsistentes como `M`, `m`, `Medium`.

## product_details

Representa el producto general o código de tienda.

Ejemplo:

```txt
store_code = 7000
name = Blusa floral
```

No representa una talla específica.

## products

Representa una variante comprada/vendible de un producto.

Ejemplo:

```txt
Blusa floral / talla M / rojo
```

Aquí se maneja stock por variante.

Campos importantes:

- `quantity`: cantidad comprada.
- `received_quantity`: cantidad físicamente recibida.
- `available_quantity`: cantidad disponible para vender.
- `reserved_quantity`: cantidad comprometida por ventas reservadas.
* `unavailable_quantity`: cantidad existente, pero temporalmente no vendible por seleccion de talla, dano, suciedad, extravio pendiente o revision.

## product_images

Imágenes asociadas al producto general.

Normalmente se relacionan con `product_details`, no con cada talla.

## clients

Información básica de clientas.

No todos los campos deben ser obligatorios porque algunas ventas pueden ser rápidas o locales.

## sales_channels

Catálogo de canales de venta.

Ejemplos:

- Local
- WhatsApp
- Instagram
- Facebook
- Web

## sale_statuses

Estados generales de una venta.

Ejemplos:

- Pending
- Reserved
- ReadyForDelivery
- SentForDelivery
- Completed
- Cancelled

## sales

Representa la venta general.

No representa pagos individuales ni envíos individuales.

Debe guardar totales congelados:

- subtotal antes de descuento
- descuento total
- subtotal de productos
- costo de envío cobrado
- total
- ganancia bruta, si decides almacenarla

## sale_details

Representa productos vendidos dentro de una venta.

Aquí deben quedar congelados:

- precio original
- descuento aplicado
- precio final
- costo histórico
- ganancia por línea
- estado de la línea


## sale_payment_statuses

Estados de pago de una venta.

Ejemplos:

- Unpaid
- PartiallyPaid
- Paid

Debe mantenerse separado de `sale_statuses`, que representa la etapa operativa.

## sale_detail_statuses

Estados para cada línea de venta.

Ejemplos:

- Active
- Cancelled
- Refunded
- Exchanged

## sale_payments

Representa pagos reales de una venta.

Una venta puede tener varios pagos.

Ejemplos:

- parte efectivo
- parte tarjeta
- pago diferido

## payment_methods

Catálogo de métodos de pago.

Ejemplos:

- Cash
- Transfer
- Card

## payment_terminals

Catálogo de POS o terminales de tarjeta.

Guarda porcentaje de comisión.

## sale_deliveries

Representa intentos de envío asociados a una venta.

Una venta puede tener varios envíos.

## delivery_statuses

Estados de envío.

Ejemplos:

- Pending
- Sent
- Delivered
- Failed
- Returned

## municipalities

Catálogo de municipios y departamentos.

Usado principalmente para envíos.

## shipping_agencies

Agencias usadas para entregar ventas a clientes.

## inventory_stock_buckets

Catalogo de buckets de inventario usados por `inventory_movements` para indicar origen y destino.

Ejemplos:

- External
- Available
- Reserved
- Unavailable
- OutOfInventory

## inventory_movement_types

Catálogo de tipos de movimiento de inventario.

Ejemplos:

- PurchaseReceived
- Sale
- Damaged
- Lost
- ReservationCreated
- ReservationReleased
- ReservationConvertedToSale
- Return
- Adjustment

## inventory_movements

Auditoría de cambios de inventario.

Debe registrar todo cambio relevante en cantidades, incluyendo bucket origen y bucket destino.

## product_holds

Representa reservas activas o históricas de productos.

Sirve para saber que productos estan apartados o enviados para seleccion sin estar vendidos. No debe usarse para productos danados, sucios o perdidos.

## product_inventory_issue_types

Catalogo de motivos operativos por los que un producto no esta vendible temporalmente.

Ejemplos:

- Damaged
- Dirty
- Missing
- UnderReview
- Repairing

## product_inventory_issue_statuses

Catalogo de estados para incidencias operativas de inventario.

Ejemplos:

- Open
- ResolvedToAvailable
- Discarded
- ConfirmedLost
- Cancelled

## product_inventory_issues

Representa productos temporalmente no disponibles por daño, suciedad, extravio pendiente, revision o reparacion.

Afecta `products.unavailable_quantity` y debe quedar respaldado por `inventory_movements`.

## financial_movement_types

Tipos de movimientos financieros.

Ejemplos:

- SalePayment
- Expense
- LoanReceived
- LoanPayment
- OwnerInvestment
- OwnerWithdrawal

## financial_movement_directions

Dirección del dinero.

Ejemplos:

- Income
- Expense

## financial_movements

Representa movimientos reales de dinero.

Debe apuntar a `sale_payment_id` cuando el movimiento viene de un pago de venta.

## expense_categories

Categorías de gastos.

Ejemplos:

- Publicidad
- Empaque
- Transporte
- Servicios
- Nómina

## loan_owners

Personas o entidades relacionadas a préstamos.

## loans

Préstamos recibidos por el negocio.

No deben mezclarse con ganancia operativa.

## loan_payments

Representa pagos aplicados a préstamos.

Guarda capital, interés, fecha, tasa de cambio y comentarios del pago.

El saldo del préstamo se calcula desde estos pagos.

## discount_campaigns

Promociones temporales.

Define nombre, fechas y si está habilitada.

## discount_campaign_products

Productos incluidos en una promoción y tipo de descuento aplicado.

## discount_types

Catálogo de tipos de descuento.

Ejemplos:

- Percentage
- FixedAmount
- FixedPrice

## discount_sources

Fuente del descuento.

Ejemplos:

- Campaign
- Manual
- None
