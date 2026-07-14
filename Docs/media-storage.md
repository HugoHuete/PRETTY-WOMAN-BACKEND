# Almacenamiento multimedia

La API usa Cloudflare R2 mediante su API compatible con S3. No se guardan binarios en PostgreSQL ni dentro del directorio desplegado de la aplicación.

## Buckets

Crear dos buckets R2:

- `pretty-woman-public-media`: variantes de catálogo que el navegador puede solicitar.
- `pretty-woman-private-media`: originales y futuros comprobantes o evidencias que nunca deben exponerse directamente.

Configurar las credenciales exclusivamente mediante secretos de entorno o User Secrets. Nunca incluirlas en `appsettings.json` ni en el repositorio.

```json
{
  "R2Media": {
    "ServiceUrl": "https://<account-id>.r2.cloudflarestorage.com",
    "AccessKeyId": "<r2-access-key-id>",
    "SecretAccessKey": "<r2-secret-access-key>",
    "PublicBucketName": "pretty-woman-public-media",
    "PrivateBucketName": "pretty-woman-private-media",
    "PublicBaseUrl": "https://images.example.com"
  }
}
```

`PublicBaseUrl` debe ser un dominio que entregue únicamente el bucket público. El bucket privado no debe tener un dominio público.

## Subida de imágenes de producto

`POST /api/v1/product-details/{productDetailId}/images` recibe `multipart/form-data` con un campo `file`.

Se admiten JPEG, PNG y WebP de hasta 8 MB. La API valida el contenido real, conserva el original en el bucket privado y genera una miniatura WebP de 400 px y una versión WebP de 1200 px en el bucket público.

La tabla `media_assets` representa el archivo lógico y `media_asset_variants` sus archivos físicos. `product_images` enlaza el producto con el recurso; esto permite reutilizar el módulo para perfiles, comprobantes y evidencias sin mezclar sus permisos.

## Migración

Antes de desplegar, generar y aplicar la migración de EF Core:

```bash
dotnet ef migrations add AddMediaAssets --project src/PrettyWoman.Infrastructure --startup-project src/PrettyWoman.Api
dotnet ef database update --project src/PrettyWoman.Infrastructure --startup-project src/PrettyWoman.Api
```
