using BlogData.Context;
using BlogData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PostsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene una lista de todas las publicaciones, opcionalmente filtradas por un término de búsqueda.
    /// </summary>
    /// <param name="search">Término opcional para buscar en el título o contenido de las publicaciones.</param>
    /// <returns>Una lista de publicaciones ordenadas por fecha de creación descendente.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Post>>> GetPosts([FromQuery] string? search = null)
    {
        try
        {
            // Clean up posts with invalid AuthorId
            var postsWithInvalidAuthors = _context.Posts.Where(p => p.AuthorId != null && !_context.Users.Any(u => u.Id == p.AuthorId));
            if (await postsWithInvalidAuthors.AnyAsync())
            {
                _context.Posts.RemoveRange(await postsWithInvalidAuthors.ToListAsync());
                await _context.SaveChangesAsync();
            }

            // Clean up likes with invalid PostId or UserId
            var likesWithInvalidPosts = _context.Likes.Where(l => !_context.Posts.Any(p => p.Id == l.PostId));
            if (await likesWithInvalidPosts.AnyAsync())
            {
                _context.Likes.RemoveRange(await likesWithInvalidPosts.ToListAsync());
                await _context.SaveChangesAsync();
            }
            var likesWithInvalidUsers = _context.Likes.Where(l => !_context.Users.Any(u => u.Id == l.UserId));
            if (await likesWithInvalidUsers.AnyAsync())
            {
                _context.Likes.RemoveRange(await likesWithInvalidUsers.ToListAsync());
                await _context.SaveChangesAsync();
            }

            var query = _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Likes) as IQueryable<Post>;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in GetPosts: {ex.ToString()}");
            return StatusCode(500, $"Internal server error: {ex.Message}, Inner: {ex.InnerException?.Message}");
        }
    }

    /// <summary>
    /// Obtiene una publicación específica por su ID.
    /// </summary>
    /// <param name="id">El ID de la publicación a obtener.</param>
    /// <returns>La publicación con el ID especificado, incluyendo autor y likes, o NotFound si no existe.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        return post;
    }

    /// <summary>
    /// Crea una nueva publicación para el usuario autenticado.
    /// </summary>
    /// <param name="post">El objeto Post con título y contenido a crear.</param>
    /// <returns>La publicación creada con autor cargado, o BadRequest/Unauthorized si hay errores de validación.</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Post>> CreatePost(Post post)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(post.Title))
            return BadRequest("Title cannot be null or empty");

        if (post.Title.Length < 3)
            return BadRequest("Title must be at least 3 characters long");

        if (post.Title.Length > 100)
            return BadRequest("Title cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(post.Content))
            return BadRequest("Content cannot be null or empty");

        if (post.Content.Length < 10)
            return BadRequest("Content must be at least 10 characters long");

        if (post.Content.Length > 5000)
            return BadRequest("Content cannot exceed 5000 characters");

        post.AuthorId = userId;
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = null;

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Load author for response
        await _context.Entry(post).Reference(p => p.Author).LoadAsync();

        return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
    }

    /// <summary>
    /// Actualiza una publicación existente con validaciones.
    /// </summary>
    /// <param name="id">El ID de la publicación a actualizar.</param>
    /// <param name="post">El objeto Post con los nuevos datos.</param>
    /// <returns>NoContent si la actualización es exitosa, BadRequest si hay errores de validación, o NotFound si no existe.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, Post post)
    {
        if (id != post.Id)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(post.Title))
            return BadRequest("Title cannot be null or empty");

        if (post.Title.Length < 3)
            return BadRequest("Title must be at least 3 characters long");

        if (post.Title.Length > 100)
            return BadRequest("Title cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(post.Content))
            return BadRequest("Content cannot be null or empty");

        if (post.Content.Length < 10)
            return BadRequest("Content must be at least 10 characters long");

        if (post.Content.Length > 5000)
            return BadRequest("Content cannot exceed 5000 characters");

        post.UpdatedAt = DateTime.UtcNow;
        _context.Entry(post).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PostExists(id))
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
    /// Elimina una publicación por su ID.
    /// </summary>
    /// <param name="id">El ID de la publicación a eliminar.</param>
    /// <returns>NoContent si la eliminación es exitosa, o NotFound si no existe.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Verifica si existe una publicación con el ID especificado.
    /// </summary>
    /// <param name="id">El ID de la publicación a verificar.</param>
    /// <returns>True si la publicación existe, false en caso contrario.</returns>
    private bool PostExists(int id)
    {
        return _context.Posts.Any(e => e.Id == id);
    }
}
