using Microsoft.AspNetCore.Identity;

namespace BlogData.Models;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Author relationship
    public string? AuthorId { get; set; }
    public IdentityUser? Author { get; set; }

    // Category relationship
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Likes relationship (computed count)
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public int LikesCount => Likes.Count;
}
