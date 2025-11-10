using BlogData.Context;
using BlogData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene una lista de todas las categorías ordenadas por nombre.
    /// </summary>
    /// <returns>Una lista de categorías ordenadas alfabéticamente por nombre.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    /// <summary>
    /// Obtiene una categoría específica por su ID.
    /// </summary>
    /// <param name="id">El ID de la categoría a obtener.</param>
    /// <returns>La categoría con el ID especificado, o NotFound si no existe.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound();
        }

        return category;
    }

    /// <summary>
    /// Crea una nueva categoría con validaciones de nombre y descripción.
    /// </summary>
    /// <param name="category">El objeto Category con nombre y descripción opcional a crear.</param>
    /// <returns>La categoría creada, o BadRequest si hay errores de validación o ya existe una categoría con el mismo nombre.</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            return BadRequest("Category name cannot be null or empty");

        if (category.Name.Length < 2)
            return BadRequest("Category name must be at least 2 characters long");

        if (category.Name.Length > 50)
            return BadRequest("Category name cannot exceed 50 characters");

        if (category.Description != null && category.Description.Length > 200)
            return BadRequest("Category description cannot exceed 200 characters");

        var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
        if (existing != null)
            return BadRequest("Category with this name already exists");

        category.CreatedAt = DateTime.UtcNow;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    /// <summary>
    /// Actualiza una categoría existente.
    /// </summary>
    /// <param name="id">El ID de la categoría a actualizar.</param>
    /// <param name="category">El objeto Category con los nuevos datos.</param>
    /// <returns>NoContent si la actualización es exitosa, BadRequest si los IDs no coinciden, o NotFound si no existe.</returns>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CategoryExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Elimina una categoría por su ID.
    /// </summary>
    /// <param name="id">El ID de la categoría a eliminar.</param>
    /// <returns>NoContent si la eliminación es exitosa, o NotFound si no existe.</returns>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Verifica si existe una categoría con el ID especificado.
    /// </summary>
    /// <param name="id">El ID de la categoría a verificar.</param>
    /// <returns>True si la categoría existe, false en caso contrario.</returns>
    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.Id == id);
    }
}
