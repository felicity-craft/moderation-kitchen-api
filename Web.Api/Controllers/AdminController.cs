using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModerationKitchen.Web.Api.Models;

namespace ModerationKitchen.Web.Api.Controllers;

[ApiController]
[Route("api/admin/recipes")]
public class AdminController : ControllerBase
{
    private readonly string dataDirectoryPath;
    private readonly IFileSystem fileSystem;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<AdminController> logger;

    public AdminController(IFileSystem fileSystem, JsonSerializerOptions jsonOptions, ILogger<AdminController> logger, IOptions<DataOptions> dataOptions)
    {
        this.fileSystem = fileSystem;
        this.jsonOptions = jsonOptions;
        this.logger = logger;
        this.dataDirectoryPath = dataOptions.Value.RecipeDirectoryPath;

    }

    [HttpGet()]
    [Route("{slug}", Order = 1)]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        this.logger.LogInformation("Trying to get recipe with slug {slug}", slug);
        string recipeFilePath = Path.Join(this.dataDirectoryPath, $"{slug}.json");
        if (this.fileSystem.File.Exists(recipeFilePath))
        {
            // get a stream which represents the file located at recipeFilePath.
            // this can be then be used by other interested parties (in this case the JsonSerializer) to read the file.
            using Stream stream = this.fileSystem.File.OpenRead(recipeFilePath);
            // turning the stream/file from text into a c# object that we can use.
            var recipe = await JsonSerializer.DeserializeAsync<Recipe>(stream, this.jsonOptions, ct);
            return this.Ok(recipe);
        }
        this.logger.LogWarning("No recipe found with slug {slug}", slug);
        return this.NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecipes([FromQuery] string? filter, CancellationToken ct)
    {
        if (this.fileSystem.Directory.Exists(this.dataDirectoryPath))
        {
            List<Recipe> recipes = await DeserializeAllRecipes(ct);
            if (filter is null) return this.Ok(recipes);
            return this.Ok(recipes.Where(recipe => recipe.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase)));
        }
        return this.Ok(Array.Empty<Recipe>());
    }

    private async Task<List<Recipe>> DeserializeAllRecipes(CancellationToken ct)
    {
        // Loops through each json file in our dataDirectory and converts the contents of each file into a Recipe object.
        // Each Recipe object is added to a single list called recipes which gets returned.
        string[] jsonFilePaths = this.fileSystem.Directory.GetFiles(this.dataDirectoryPath, "*.json");
        var recipes = new List<Recipe>();
        foreach (var filePath in jsonFilePaths)
        {
            using Stream stream = this.fileSystem.File.OpenRead(filePath);
            var recipe = await JsonSerializer.DeserializeAsync<Recipe>(stream, this.jsonOptions, ct);
            recipes.Add(recipe!);
        }

        return recipes.OrderByDescending(recipe => recipe.Date).ToList();
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedRecipes([FromQuery] int limit, CancellationToken ct)
    {
        if (this.fileSystem.Directory.Exists(this.dataDirectoryPath))
        {
            var recipes = await this.DeserializeAllRecipes(ct);
            return this.Ok(recipes.Take(limit));
        }
        return this.Ok(Array.Empty<Recipe>());
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] Recipe recipe, CancellationToken ct)
    {
        string recipeFilePath = Path.Join(this.dataDirectoryPath, $"{recipe.Slug}.json");
        if (this.fileSystem.File.Exists(recipeFilePath))
        {
            return this.Conflict();
        }
        using Stream stream = this.fileSystem.File.OpenWrite(recipeFilePath);
        await JsonSerializer.SerializeAsync<Recipe>(stream, recipe, this.jsonOptions, ct);
        return this.Created($"/api/recipes/{recipe.Slug}", recipe);
    }

    [HttpPut ("{slug}")]
    public async Task<IActionResult> UpdateRecipe([FromRoute] string slug,[FromBody] Recipe recipe, CancellationToken ct) {

        this.logger.LogInformation("Trying to update recipe with slug {slug}", slug);
        string recipeFilePath = Path.Join(this.dataDirectoryPath, $"{slug}.json");
        if (this.fileSystem.File.Exists(recipeFilePath))
        {
            using Stream stream = this.fileSystem.File.Open(recipeFilePath, FileMode.Truncate);
           await JsonSerializer.SerializeAsync<Recipe>(stream, recipe, this.jsonOptions, ct);
           return NoContent();
        }
        this.logger.LogWarning("No recipe found with slug {slug}", slug);
        return this.NotFound();
    }



    [HttpDelete("{slug}")]
    public IActionResult DeleteRecipe([FromRoute] string slug)
    {
        string recipeFilePath = Path.Join(this.dataDirectoryPath, $"{slug}.json");
        if (this.fileSystem.File.Exists(recipeFilePath))
        {
            this.fileSystem.File.Delete(recipeFilePath);
            return this.NoContent();
        }
        this.logger.LogWarning("Cannot delete recipe. No recipe found with slug {slug}", slug);
        return this.NotFound();
    }

}