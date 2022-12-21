namespace ModerationKitchen.Web.Api.Models;

public record RecipeComment(
    double Rating,
    string? Comment,
    string Name,
    string Email,
    DateTime Date
);