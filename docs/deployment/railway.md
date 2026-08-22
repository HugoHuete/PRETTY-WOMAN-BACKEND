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
| `ConnectionStrings__DefaultConnection` | Referencia a la URL de conexión interna del servicio PostgreSQL de Railway. |
| `Jwt__Key` | Secreto aleatorio de al menos 32 caracteres. |
| `Jwt__Issuer` | Identificador del emisor de tokens, por ejemplo `PrettyWoman.Api`. |
| `Jwt__Audience` | Identificador del cliente que consume los tokens. |
| `Cors__AdminOrigins__0` | Origen HTTPS exacto del frontend administrativo, por ejemplo `https://admin.example.com`. |
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

Para `ConnectionStrings__DefaultConnection`, usa una referencia de variable al servicio PostgreSQL, no una URL pública ni una credencial escrita en el repositorio. En el panel de Railway, selecciona la variable de conexión que proporciona el servicio PostgreSQL para que Railway la inyecte en la API.

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

Railway termina TLS antes de reenviar la solicitud a la API. La aplicación procesa los encabezados `X-Forwarded-For` y `X-Forwarded-Proto` en Production antes de redirigir HTTP a HTTPS.

## Archivos e imágenes

No uses el filesystem del contenedor para imágenes, comprobantes ni datos de negocio: Railway puede reemplazar el contenedor en cualquier despliegue o reinicio. La implementación actual envía originales y variantes a Cloudflare R2; los streams de subida y la exportación XLSX son temporales y no se persisten localmente.

ASP.NET Core Data Protection usa un key ring temporal en el contenedor. Esto no afecta los JWT emitidos, pero tokens basados en Data Protection (por ejemplo, recuperación de contraseña) se invalidan tras un redeploy y no se comparten entre réplicas. Si se habilitan esos flujos o se escala la API, persiste esas claves en un almacén compartido antes de hacerlo.
