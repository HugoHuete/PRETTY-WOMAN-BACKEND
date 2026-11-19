using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrettyWoman.Application.Services;

namespace PrettyWoman.Api.Controllers;

[AllowAnonymous]
[Route("compartir")]
public sealed class CatalogShareController(PublicCatalogService catalog, IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
{
    private string Origin => (configuration["Catalog:Origin"] ??
        (environment.IsDevelopment() ? "http://localhost:5173" : "https://catalog.prettywomanboutiquenic.com")).TrimEnd('/');

    [HttpGet("prendas/{id:int}")]
    public async Task<IActionResult> Product(int id, CancellationToken cancellationToken)
    {
        var product = await catalog.GetAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        return Preview(product.Name, product.SoldOut ? "Prenda agotada · Pretty Woman Boutique" : "Ver prenda en Pretty Woman Boutique",
            product.Images.FirstOrDefault()?.Url ?? $"{Origin}/logo.png", $"{Origin}/prendas/{id}");
    }

    [HttpGet("selecciones/{id:guid}")]
    public IActionResult Selection(Guid id) => Preview("Selección de prendas · Pretty Woman",
        "Abre el catálogo para consultar esta selección.", $"{Origin}/logo.png", $"{Origin}/selecciones/{id}");

    private ContentResult Preview(string title, string description, string image, string target)
    {
        string Encode(string value) => HtmlEncoder.Default.Encode(value);
        Response.Headers["X-Robots-Tag"] = "noindex";
        return Content($"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <title>{Encode(title)}</title>
              <meta property="og:type" content="website">
              <meta property="og:title" content="{Encode(title)}">
              <meta property="og:description" content="{Encode(description)}">
              <meta property="og:image" content="{Encode(image)}">
              <meta name="referrer" content="no-referrer">
              <meta http-equiv="refresh" content="0;url={Encode(target)}">
            </head>
            <body><a href="{Encode(target)}">Abrir en Pretty Woman Boutique</a></body>
            </html>
            """, "text/html; charset=utf-8");
    }
}
