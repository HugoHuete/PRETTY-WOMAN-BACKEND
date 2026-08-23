# Despliegue en Railway

La API se despliega desde la raíz del repositorio mediante el `Dockerfile` incluido. El repositorio usa .NET 10, por lo que la imagen se construye con .NET SDK 10 y se ejecuta con ASP.NET Core Runtime 10.

## Servicios

1. Crea un servicio PostgreSQL en el proyecto Railway.
2. Crea un servicio para este repositorio y deja que Railway detecte `Dockerfile`.
3. Agrega un dominio público al servicio de API.
4. Configura el health check como `/health`; `railway.json` ya lo declara.

`PrettyWoman.Workers` no se inicia en la imagen de API. Si los workers deben ejecutarse en producción, crea un segundo servicio Railway desde el mismo repositorio con su propia imagen o comando de inicio.

## Variables de Railway

Configura estas variables en el servicio de API. Los nombres con `__` se convierten a secciones anidadas de configuración de ASP.NET Core.

| Variable | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true` (sustituye `Postgres` por el nombre exacto del servicio). |
| `Jwt__Key` | Secreto aleatorio de al menos 32 caracteres. |
| `Jwt__Issuer` | Identificador del emisor de tokens, por ejemplo `PrettyWoman.Api`. |
| `Jwt__Audience` | Identificador del cliente que consume los tokens. |
| `Cors__AdminOrigins__0` | Origen HTTPS exacto del frontend administrativo, por ejemplo `https://admin.example.com`. |
| `SeedAdmin__Username` | Nombre de usuario del administrador inicial, usado para iniciar sesión. |
| `SeedAdmin__Email` | Correo del administrador inicial, solo mientras la base todavía no tenga administrador. |
| `SeedAdmin__Password` | Contraseña fuerte del administrador inicial. |
| `SeedAdmin__Name` | Opcional; nombre del administrador inicial. |
| `SeedAdmin__Lastname` | Opcional; apellido del administrador inicial. |
| `R2Media__ServiceUrl` | Endpoint S3 de Cloudflare R2. |
| `R2Media__AccessKeyId` | Access key de R2. |
| `R2Media__SecretAccessKey` | Secret access key de R2. |
| `R2Media__PublicBucketName` | Bucket público de variantes de catálogo. |
| `R2Media__PrivateBucketName` | Bucket privado para originales y evidencias. |
| `R2Media__PublicBaseUrl` | Dominio HTTPS que expone exclusivamente el bucket público. |

Railway proporciona `PORT` automáticamente. La aplicación lo usa para escuchar en `0.0.0.0`; no configures un valor fijo. `ASPNETCORE_URLS` es opcional porque el contenedor usa `http://+:8080` fuera de Railway y `PORT` tiene precedencia en Railway.

Para `ConnectionStrings__DefaultConnection`, usa referencias de variables al servicio PostgreSQL, no una URL pública ni una credencial escrita en el repositorio. `DATABASE_URL` usa el formato `postgresql://...`, que no es el formato de cadena de conexión que espera Npgsql.

## Rate limiting

La API limita solicitudes globalmente por tipo de operación, con una ventana deslizante de un minuto. Solo existen cuatro buckets de límite, por lo que no se acumula estado por cada IP que contacte la API. Los valores predeterminados son:

| Tipo de solicitud | Límite por IP |
| --- | ---: |
| `POST /api/v1/auth/login` | 30 por minuto |
| Lecturas `GET` y `HEAD` | 300 por minuto |
| Escrituras | 100 por minuto |
| Creación o modificación de imágenes | 10 por minuto |
| Health checks | Sin límite |

Puedes ajustar los valores mediante variables opcionales de Railway, por ejemplo `RateLimiting__ReadPermitLimit=180` o `RateLimiting__ImagePermitLimit=5`. También están disponibles `RateLimiting__LoginPermitLimit`, `RateLimiting__WritePermitLimit`, `RateLimiting__WindowSeconds` y `RateLimiting__SegmentsPerWindow`.

El contador vive en memoria de cada instancia. Con una sola réplica de Railway funciona como se describe; si escalas a varias réplicas y necesitas un límite global, se deberá usar un almacén compartido como Redis.

## Migraciones

La imagen genera un bundle autocontenido de EF Core llamado `efbundle`. Railway ejecuta `./efbundle` como **pre-deploy command** antes de iniciar una nueva instancia de la API. El bundle lee `ConnectionStrings__DefaultConnection` desde el entorno.

Si una migración falla, Railway no publica la nueva versión. La API no ejecuta `Database.Migrate()` al arrancar; así se evita que varias réplicas intenten cambiar el esquema a la vez.

Antes de desplegar una migración, ejecuta localmente:

```bash
make pending-model-changes
make migrate
```

## Health checks y HTTPS

- `GET /health/live` verifica que el proceso está vivo, sin acceder a dependencias.
- `GET /health/ready` verifica la conexión con PostgreSQL.
- `GET /health` es el endpoint de Railway y también verifica PostgreSQL.

Railway termina TLS antes de reenviar la solicitud a la API. La aplicación procesa `X-Forwarded-Proto` únicamente desde el rango de proxy recomendado por Railway (`100.0.0.0/8`) para redirigir HTTP a HTTPS. El rate limiting no usa encabezados ni direcciones IP del cliente.

## Archivos e imágenes

No uses el filesystem del contenedor para imágenes, comprobantes ni datos de negocio: Railway puede reemplazar el contenedor en cualquier despliegue o reinicio. La implementación actual envía originales y variantes a Cloudflare R2; los streams de subida y la exportación XLSX son temporales y no se persisten localmente.

ASP.NET Core Data Protection usa un key ring temporal en el contenedor. Esto no afecta los JWT emitidos, pero tokens basados en Data Protection (por ejemplo, recuperación de contraseña) se invalidan tras un redeploy y no se comparten entre réplicas. Si se habilitan esos flujos o se escala la API, persiste esas claves en un almacén compartido antes de hacerlo.
