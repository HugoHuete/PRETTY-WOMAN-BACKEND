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

`POST /api/v1/products/{productId}/images` recibe `multipart/form-data` con un campo `file`. Para asociarla a un color o presentación, agregar `?productPresentationId={id}`; si se omite, la imagen pertenece al producto general.

Se admiten JPEG, PNG y WebP de hasta 8 MB. La API valida el contenido real, conserva el original en el bucket privado y genera una miniatura WebP de 400 px y una versión WebP de 1200 px en el bucket público.

Para actualizar en una sola operación el orden y la imagen principal del producto general, usar `PUT /api/v1/products/{productId}/images`:

```json
{
  "primaryImageId": 42,
  "imageIdsInOrder": [42, 38, 51]
}
```

Para una presentación, incluir `productPresentationId` en el cuerpo:

```json
{
  "productPresentationId": 31,
  "primaryImageId": 52,
  "imageIdsInOrder": [52, 53]
}
```

Cada presentación puede tener su propia imagen principal. `product_images.product_presentation_id` nullable distingue las imágenes generales de las específicas; no es necesario crear una presentación artificial para productos de un solo tono.

La lista debe incluir exactamente todas las imágenes actuales del producto, sin IDs repetidos. Para quitar una imagen, usar `DELETE /api/v1/products/{productId}/images/{imageId}`. Si era la principal, la siguiente según el orden pasa a ser la principal.

La tabla `media_assets` representa el archivo lógico y `media_asset_variants` sus archivos físicos. `product_images` enlaza el producto con el recurso y opcionalmente con `product_presentations`; esto permite reutilizar el módulo para perfiles, comprobantes y evidencias sin mezclar sus permisos.

## Migración

Antes de desplegar, generar y aplicar la migración de EF Core:

```bash
dotnet ef migrations add AddMediaAssets --project src/PrettyWoman.Infrastructure --startup-project src/PrettyWoman.Api
dotnet ef database update --project src/PrettyWoman.Infrastructure --startup-project src/PrettyWoman.Api
```
