using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModerationKitchen.Web.Api.Models;

namespace ModerationKitchen.Web.Api.Controllers;

[ApiController]
public class CommentsController : ControllerBase
{
    private readonly string dataDirectoryPath = "/Users/fliss/Desktop/VS Projects/ModerationKitchen/WebApi/Web.Api/Data/Recipes";
    private readonly IFileSystem fileSystem;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<CommentsController> logger;

    public CommentsController(IFileSystem fileSystem, JsonSerializerOptions jsonOptions, ILogger<CommentsController> logger)
    {
        this.fileSystem = fileSystem;
        this.jsonOptions = jsonOptions;
        this.logger = logger;
    }

    [HttpPost("api/recipes/{slug}/comments")]
    public async Task<IActionResult> CreateComment([FromRoute] string slug, [FromBody] RecipeComment recipeComment, CancellationToken ct)
    {
        this.logger.LogInformation("Trying to get recipe with slug {slug}", slug);
        string recipeFilePath = Path.Join(this.dataDirectoryPath, $"{slug}.json");
        if (this.fileSystem.File.Exists(recipeFilePath))
        {
            Recipe? recipe = null;
            using (Stream stream = this.fileSystem.File.OpenRead(recipeFilePath))
            {
                recipe = await JsonSerializer.DeserializeAsync<Recipe>(stream, this.jsonOptions, ct);
                recipe.Comments = recipe.Comments ?? new List<RecipeComment>();
                var newComments = recipe.Comments.Append(recipeComment);
                recipe.Comments = newComments.OrderByDescending(c => c.Date).ToList();
            }
            using (Stream stream = this.fileSystem.File.Open(recipeFilePath, FileMode.Truncate))
            {
                await JsonSerializer.SerializeAsync<Recipe>(stream, recipe, this.jsonOptions, ct);
            }
            return this.NoContent();
        }
        this.logger.LogWarning("No recipe found with slug {slug}", slug);
        return this.NotFound();
    }
}