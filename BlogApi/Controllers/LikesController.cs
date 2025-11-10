using BlogData.Context;
using BlogData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LikesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene una lista de likes para una publicación específica.
    /// </summary>
    /// <param name="postId">El ID de la publicación para la cual obtener los likes.</param>
    /// <returns>Una lista de likes con información del usuario que dio like.</returns>
    [HttpGet("post/{postId}")]
    public async Task<ActionResult<IEnumerable<Like>>> GetLikesForPost(int postId)
    {
        var likes = await _context.Likes
            .Where(l => l.PostId == postId)
            .Include(l => l.User)
            .ToListAsync();

        return likes;
    }

    /// <summary>
    /// Da like a una publicación específica.
    /// </summary>
    /// <param name="postId">El ID de la publicación a la cual dar like.</param>
    /// <returns>Ok con mensaje de éxito, o BadRequest/Unauthorized/NotFound si hay errores.</returns>
    [HttpPost("post/{postId}")]
    [Authorize]
    public async Task<IActionResult> LikePost(int postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("User not authenticated");
        }

        if (postId <= 0)
            return BadRequest("Invalid post ID");

        // Check if post exists
        var post = await _context.Posts.FindAsync(postId);
        if (post == null)
        {
            return NotFound("Post not found");
        }

        if (post.AuthorId == userId)
            return BadRequest("You cannot like your own post");

        // Check if user already liked this post
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike != null)
        {
            return BadRequest("You have already liked this post");
        }

        // Create new like
        var like = new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Likes.Add(like);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Post liked successfully" });
    }

    /// <summary>
    /// Quita el like de una publicación específica.
    /// </summary>
    /// <param name="postId">El ID de la publicación de la cual quitar el like.</param>
    /// <returns>Ok con mensaje de éxito, o Unauthorized/NotFound si hay errores.</returns>
    [HttpDelete("post/{postId}")]
    [Authorize]
    public async Task<IActionResult> UnlikePost(int postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (like == null)
        {
            return NotFound("Like not found");
        }

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Post unliked successfully" });
    }

    /// <summary>
    /// Verifica si el usuario actual ha dado like a una publicación específica.
    /// </summary>
    /// <param name="postId">El ID de la publicación a verificar.</param>
    /// <returns>Un objeto con HasLiked indicando si el usuario dio like, o Unauthorized si no está autenticado.</returns>
    [HttpGet("post/{postId}/status")]
    [Authorize]
    public async Task<IActionResult> GetLikeStatus(int postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        return Ok(new { HasLiked = like != null });
    }
}
