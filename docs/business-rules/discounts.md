# Discounts Business Rules

## Objetivo

Manejar promociones temporales y descuentos manuales, permitiendo calcular precio final, descuentos por producto y ganancia por línea.

## Tablas principales

- `discount_campaigns`
- `discount_campaign_products`
- `discount_type`
- `discount_source`
- `sale_details`
- `sales`

## Regla: promociones temporales

Una promoción temporal se registra en `discount_campaigns`.

Debe tener:

- nombre
- fecha de inicio
- fecha final
- estado habilitado/deshabilitado

Solo debe aplicar si:

```txt
enabled = true
current_date >= start_date
current_date <= end_date
```

## Regla: productos en promoción

`discount_campaign_products` define el destino y descuento de cada regla de una campania. Una regla debe apuntar exactamente a uno de estos destinos:

- `product_id`: aplica a todas las variantes (`product_variants`) del producto.
- `product_variant_id`: aplica unicamente a esa variante/talla.

El frontend puede enviar cualquiera de los dos identificadores, pero no ambos ni ninguno. Cuando varias reglas activas aplican a una variante, se utiliza la que produzca el menor precio final.

Cada registro debe indicar:

- campaña
- producto general (`product_id`) o variante específica (`product_variant_id`)
- tipo de descuento
- valor del descuento

Tipos sugeridos:

- `Percentage`: porcentaje.
- `FixedAmount`: monto fijo descontado.
- `FixedPrice`: precio final fijo.

## Regla: evitar promociones traslapadas

Un mismo destino no debe repetirse dentro de una campaña.

La primera versión puede validar esto desde la app antes de crear una promoción.

Regla recomendada:

```txt
No permitir dos reglas con el mismo destino dentro de una campaña.
```

Una campaña puede combinar reglas globales y reglas específicas; el precio de la variante se resuelve con la mejor regla activa.

## Regla: consistencia entre monto, fuente y campaña

- Si `discount_amount > 0`, `discount_source_id` es obligatorio y no puede ser `None`.
- Si la fuente es `Manual`, `discount_campaign_id` debe ser `NULL`. El monto se registra explícitamente en la venta.
- Si la fuente es `Campaign`, la campaña es obligatoria y el descuento debe calcularse desde la campaña aplicable; el monto resultante se congela en la línea histórica.
- Si no hay descuento (`discount_amount = 0`), la única fuente permitida es `None` y `discount_campaign_id` debe ser `NULL`.

## Regla: descuentos manuales

Un descuento manual es aquel dado por decisión de venta en el momento.

Ejemplos:

- Clienta compró varias prendas.
- Producto con detalle menor.
- Descuento autorizado por encargada.

No requiere campaña.

Debe guardarse en la línea de venta con:

- `discount_source_id = Manual`
- `discount_campaign_id = NULL`
- `discount_amount`
- comentario o razón si aplica

## Regla: descuentos globales deben prorratearse

Aunque el descuento se dé sobre la compra completa, debe distribuirse entre las líneas de venta para poder calcular ganancia por producto.

Ejemplo:

```txt
Producto A: C$500
Producto B: C$300
Producto C: C$200
Subtotal: C$1000
Descuento global: C$100
```

Prorrateo proporcional:

```txt
Producto A: C$50
Producto B: C$30
Producto C: C$20
```

Cada línea debe guardar su propio `discount_amount`.

## Regla: descuentos se guardan en `sale_details`

Para la primera versión no se usarán tablas `sale_discounts` ni `sale_detail_discounts`.

Cada línea debe guardar:

- precio original
- fuente del descuento
- campaña si aplica
- monto descontado
- precio final

## Regla: totales de descuento en `sales`

`sales` debe guardar resumen:

- `subtotal`
- `total_discount`
- `subtotal`

Donde:

```txt
subtotal = suma de precios originales * cantidad
total_discount = suma de descuentos por línea
total = subtotal - total_discount
```

## Regla: precio histórico

La venta debe guardar el precio final al momento de vender.

Nunca se debe recalcular una venta histórica usando el precio actual del producto o la campaña actual.

## Regla: productos vendidos con descuento

Para saber cuántos productos se vendieron con descuento:

- contar líneas con `discount_amount > 0`
- o sumar unidades de esas líneas

Esto funciona porque todo descuento global debe quedar prorrateado por línea.

