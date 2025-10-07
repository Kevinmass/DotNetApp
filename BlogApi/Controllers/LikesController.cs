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

    // GET: api/likes/post/5 - Get likes for a specific post
    [HttpGet("post/{postId}")]
    public async Task<ActionResult<IEnumerable<Like>>> GetLikesForPost(int postId)
    {
        var likes = await _context.Likes
            .Where(l => l.PostId == postId)
            .Include(l => l.User)
            .ToListAsync();

        return likes;
    }

    // POST: api/likes/post/5 - Like a post
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

    // DELETE: api/likes/post/5 - Unlike a post
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

    // GET: api/likes/post/5/status - Check if current user liked a post
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
