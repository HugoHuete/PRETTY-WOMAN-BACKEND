# Aplicar descuento manual

## Objetivo

Registrar un descuento otorgado en el momento de la venta.

## Cuándo aplica

- Cliente compra muchos productos.
- Vendedora autoriza descuento.
- Producto tiene detalle menor.
- Se aplica descuento especial no asociado a campaña.

## Tablas involucradas

- `sales`
- `sale_details`
- `discount_sources`
- `discount_types`

## Flujo esperado

1. Recibir descuento manual por línea o global.
2. Si es por línea:
   - calcular `discount_amount` de esa línea.
3. Si es global:
   - prorratear el descuento entre las líneas de venta.
4. Guardar en cada `sale_detail`:
   - `discount_amount`
   - `final_sale_price`
   - `discount_source_id = Manual`
   - `discount_reason`
5. Actualizar en `sales`:
   - `subtotal_before_discount`
   - `total_discount`
   - `subtotal_products`
   - `gross_profit`

## Regla clave

Aunque el descuento sea global, debe distribuirse entre las líneas para poder calcular ganancia por producto.

## Ejemplo

Venta:

```txt
Producto A: C$500
Producto B: C$300
Producto C: C$200
Subtotal: C$1,000
Descuento manual global: C$100
```

Prorrateo:

```txt
Producto A: C$50
Producto B: C$30
Producto C: C$20
```

## Errores esperados

- Descuento mayor que subtotal.
- Línea sin precio.
- Prorrateo no cuadra por redondeo.
- Motivo requerido no enviado, si decides hacerlo obligatorio.
