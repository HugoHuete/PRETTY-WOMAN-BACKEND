# Frontend Handoff - Pretty Woman Admin

Este documento sirve como puente entre el backend y el futuro frontend administrativo.
Por ahora vive en el repo del backend porque aqui estan las reglas de negocio, casos de uso y contratos API.
Cuando exista `Pretty-Woman_Frontend_Admin`, puede copiarse o dividirse alla en documentos de producto/UI.

## Alcance inicial

El frontend inicial sera una aplicacion administrativa interna en React.
No incluye tienda publica para clientas en esta etapa.

Si mas adelante se construye una experiencia publica para clientas, conviene crear otro proyecto separado, por ejemplo `Pretty-Woman_Frontend_Store`, porque tendra objetivos distintos: catalogo visual, carrito, checkout, SEO y rutas publicas.

## Usuarios

### Admin

Responsable de administrar el sistema completo.

Puede trabajar con:

- Usuarios y accesos.
- Productos, categorias, subcategorias y tallas.
- Proveedores y compras.
- Inventario.
- Ventas, reservas, descuentos, cambios y cancelaciones.
- Clientes.
- Finanzas, gastos, prestamos y reportes.
- Catalogos operativos como agencias de envio, terminales de pago y categorias de gasto.

### Vendedor

Responsable de la operacion diaria de venta.

Puede trabajar con:

- Login.
- Busqueda de productos.
- Consulta de stock disponible.
- Clientes.
- Creacion de ventas.
- Reservas de productos.
- Pagos de venta.
- Entregas/envios.
- Cambios o cancelaciones segun permisos definidos.

## Modulos del frontend admin

| Modulo | Objetivo | Roles principales | Estado esperado |
|---|---|---|---|
| Autenticacion | Iniciar sesion y administrar acceso | Admin, Vendedor | Necesario para MVP |
| Dashboard | Ver resumen operativo del negocio | Admin, Vendedor | Necesario para MVP |
| Productos | Consultar catalogo, detalle, filtros y stock | Admin, Vendedor | Necesario para MVP |
| Catalogos | Gestionar categorias, subcategorias y tallas | Admin | Necesario para inventario |
| Clientes | Crear, editar, bloquear y consultar clientas | Admin, Vendedor | Necesario para ventas |
| Compras | Crear ordenes, tracking y recepcion de productos | Admin | Necesario para inventario |
| Inventario | Ver disponibilidad y problemas de inventario | Admin, Vendedor | Necesario para operacion |
| Ventas | Crear ventas, detalles, pagos y entregas | Admin, Vendedor | Necesario para MVP |
| Reservas | Apartar productos, liberar o convertir en venta | Admin, Vendedor | Necesario para operacion |
| Descuentos | Gestionar campanas y descuentos manuales | Admin, Vendedor con permisos | Necesario para ventas |
| Finanzas | Ver balance, movimientos, gastos y prestamos | Admin | Posterior al MVP si se prioriza ventas primero |
| Configuracion | Agencias, terminales, proveedores y categorias de gasto | Admin | Segun necesidad operativa |

## Mapa inicial de pantallas

| Modulo | Pantalla | Roles | Acciones principales | Datos necesarios | Endpoints relacionados |
|---|---|---|---|---|---|
| Autenticacion | Login | Admin, Vendedor | Iniciar sesion | Credenciales | `POST /api/v1/auth/login` |
| Autenticacion | Usuarios | Admin | Crear usuario, desbloquear usuario | Usuarios, roles/permisos | `POST /api/v1/auth/users`, `POST /api/v1/auth/users/{id}/unlock` |
| Dashboard | Resumen | Admin, Vendedor | Ver ventas, pagos, stock y alertas | Balance, movimientos, ventas, inventario | Pendiente de endpoint especifico |
| Productos | Lista de productos | Admin, Vendedor | Buscar, filtrar, paginar, abrir detalle | Productos, categoria, subcategoria, talla, stock | `GET /api/v1/product-details` |
| Productos | Detalle de producto | Admin, Vendedor | Ver informacion completa y disponibilidad | Producto, variantes/detalle, stock | `GET /api/v1/product-details/{productDetailId}` |
| Catalogos | Categorias | Admin | Listar, crear, editar | Categorias | `GET /api/v1/categories`, `POST /api/v1/categories`, `PUT /api/v1/categories/{id}` |
| Catalogos | Subcategorias | Admin | Listar, filtrar por categoria, crear, editar | Subcategorias, categorias | `GET /api/v1/subcategories`, `GET /api/v1/categories/{id}/subcategories`, `POST /api/v1/subcategories`, `PUT /api/v1/subcategories/{id}` |
| Catalogos | Tallas | Admin | Listar, crear, editar | Tallas | `GET /api/v1/sizes`, `POST /api/v1/sizes`, `PUT /api/v1/sizes/{id}` |
| Clientes | Lista de clientes | Admin, Vendedor | Buscar, ver detalle, crear, editar | Clientes | `GET /api/v1/clients`, `POST /api/v1/clients`, `PUT /api/v1/clients/{id}` |
| Clientes | Estado de cliente | Admin | Bloquear, desbloquear | Cliente, motivo de bloqueo | `PATCH /api/v1/clients/{id}/block`, `PATCH /api/v1/clients/{id}/unblock` |
| Compras | Ordenes | Admin | Listar, crear, editar, ver detalle | Ordenes, proveedores, tracking | `GET /api/v1/orders`, `POST /api/v1/orders`, `PUT /api/v1/orders/{id}` |
| Compras | Tracking de orden | Admin | Agregar, editar, eliminar tracking | Orden, numeros de tracking | `GET /api/v1/orders/{id}/tracking-numbers`, `POST /api/v1/orders/{id}/tracking-numbers`, `PUT /api/v1/orders/{id}/tracking-numbers/{trackingId}`, `DELETE /api/v1/orders/{id}/tracking-numbers/{trackingId}` |
| Compras | Recepcion de productos | Admin | Registrar recepcion de productos de una orden | Orden, productos recibidos, cantidades | `POST /api/v1/orders/{orderId}/receipts` |
| Proveedores | Proveedores | Admin | Listar, crear, editar | Proveedores | `GET /api/v1/suppliers`, `POST /api/v1/suppliers`, `PUT /api/v1/suppliers/{id}` |
| Descuentos | Campanas | Admin | Listar, crear, editar, deshabilitar | Campanas de descuento | `GET /api/v1/discountcampaigns`, `POST /api/v1/discountcampaigns`, `PUT /api/v1/discountcampaigns/{id}`, `PATCH /api/v1/discountcampaigns/{id}/disable` |
| Finanzas | Balance | Admin | Ver balance actual | Balance financiero | `GET /api/v1/finances/current-balance` |
| Finanzas | Movimientos | Admin | Listar, filtrar, crear, editar, eliminar | Movimientos, tipos de movimiento | `GET /api/v1/finances/movements`, `POST /api/v1/finances/movements`, `PUT /api/v1/finances/movements/{id}`, `DELETE /api/v1/finances/movements/{id}` |
| Finanzas | Prestamos | Admin | Listar, crear, editar, eliminar, registrar pagos | Prestamos, duenos, pagos | `GET /api/v1/loans`, `POST /api/v1/loans`, `PUT /api/v1/loans/{id}`, `DELETE /api/v1/loans/{id}`, `POST /api/v1/loans/{id}/payments` |
| Finanzas | Duenos de prestamo | Admin | Listar, crear, editar | Duenos de prestamo | `GET /api/v1/loanowners`, `POST /api/v1/loanowners`, `PUT /api/v1/loanowners/{id}` |
| Configuracion | Agencias de envio | Admin | Listar, crear, editar | Agencias de envio | `GET /api/v1/deliveryagencies`, `POST /api/v1/deliveryagencies`, `PUT /api/v1/deliveryagencies/{id}` |
| Configuracion | Terminales de pago | Admin | Listar, crear, editar | Terminales POS | `GET /api/v1/paymentterminals`, `POST /api/v1/paymentterminals`, `PUT /api/v1/paymentterminals/{id}` |
| Configuracion | Categorias de gasto | Admin | Listar, crear, editar | Categorias de gasto | `GET /api/v1/expensecategories`, `POST /api/v1/expensecategories`, `PUT /api/v1/expensecategories/{id}` |

## Pantallas planeadas que dependen de endpoints por confirmar

Estas pantallas aparecen en las reglas de negocio y casos de uso, pero deben confirmarse contra controladores/endpoints finales antes de implementar el frontend.

| Modulo | Pantalla/flujo | Documentacion fuente |
|---|---|---|
| Ventas | Crear venta | `Docs/use-cases/create-sale.md`, `Docs/business-rules/sales.md` |
| Ventas | Crear pago de venta | `Docs/use-cases/create-sale-payment.md`, `Docs/business-rules/payments.md` |
| Ventas | Crear entrega/envio | `Docs/use-cases/create-sale-delivery.md`, `Docs/business-rules/deliveries.md` |
| Ventas | Cancelar venta | `Docs/use-cases/cancel-sale.md`, `Docs/business-rules/returns-and-exchanges.md` |
| Ventas | Cancelar detalle de venta | `Docs/use-cases/cancel-sale-detail.md`, `Docs/business-rules/returns-and-exchanges.md` |
| Reservas | Crear reserva | `Docs/use-cases/create-product-hold.md`, `Docs/business-rules/product-holds.md` |
| Reservas | Liberar reserva | `Docs/use-cases/release-product-hold.md`, `Docs/business-rules/product-holds.md` |
| Reservas | Convertir reserva en venta | `Docs/use-cases/convert-product-hold-to-sale.md`, `Docs/business-rules/product-holds.md` |
| Descuentos | Aplicar descuento manual | `Docs/use-cases/apply-manual-discount.md`, `Docs/business-rules/discounts.md` |
| Descuentos | Aplicar descuento de campana | `Docs/use-cases/apply-campaign-discount.md`, `Docs/business-rules/discounts.md` |
| Inventario | Registrar producto danado | `Docs/use-cases/register-damaged-product.md`, `Docs/business-rules/product-inventory-issues.md` |
| Inventario | Registrar producto perdido | `Docs/use-cases/register-lost-product.md`, `Docs/business-rules/product-inventory-issues.md` |
| Inventario | Registrar producto descartado | `Docs/use-cases/register-discarded-product.md`, `Docs/business-rules/product-inventory-issues.md` |

## Reglas que el frontend debe respetar

- La UI no debe calcular reglas criticas de negocio como totales finales, stock disponible, prorrateos de descuento, comisiones o estados finales. Debe mostrar lo que devuelve el backend.
- El frontend puede hacer validaciones rapidas de formulario, pero el backend sigue siendo la fuente de verdad.
- Las acciones destructivas o sensibles deben pedir confirmacion visual: cancelar venta, eliminar movimiento financiero, desbloquear usuario, bloquear cliente, descartar producto.
- El vendedor debe ver solo acciones operativas necesarias para vender, reservar, cobrar y entregar.
- El admin debe poder ver y gestionar configuraciones, finanzas y catalogos.
- Las pantallas de venta y reserva deben dejar claro el estado del producto: disponible, reservado, vendido, no disponible o pendiente de recepcion.

## Decisiones tomadas

- El frontend inicial sera solo administrativo.
- Los roles iniciales son Admin y Vendedor.
- La tienda publica para clientas, si se construye, ira en otro proyecto.
- El mapa de pantallas puede empezar en este backend como handoff y moverse al repo frontend cuando exista.

## Pendientes para iniciar React

- Confirmar stack: Vite + React, TypeScript, router, libreria UI y manejo de estado.
- Definir si se usara Tailwind/shadcn, MUI u otra libreria.
- Definir permisos exactos por rol.
- Confirmar endpoints faltantes para ventas, reservas e inventario operativo.
- Crear `Pretty-Woman_Frontend_Admin`.
- Mover o copiar este documento a `Pretty-Woman_Frontend_Admin/Docs/product/admin-screen-map.md`.
- Definir layout base: sidebar, topbar, area de contenido, tablas, formularios y modales.

