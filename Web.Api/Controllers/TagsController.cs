using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ModerationKitchen.Web.Api.Models;

namespace ModerationKitchen.Web.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly string dataDirectoryPath = "/Users/fliss/Desktop/VS Projects/ModerationKitchen/WebApi/Web.Api/Data/Recipes";
    private readonly IFileSystem fileSystem;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<TagsController> logger;

    public TagsController(IFileSystem fileSystem, JsonSerializerOptions jsonOptions, ILogger<TagsController> logger)
    {
        this.fileSystem = fileSystem;
        this.jsonOptions = jsonOptions;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecipes([FromQuery] string? filter, CancellationToken ct)
    {
        if (this.fileSystem.Directory.Exists(this.dataDirectoryPath))
        {
            var recipes = await DeserializeAllRecipes(ct);
            var tags = recipes.SelectMany(recipe => recipe.Tags).Distinct().ToList();
            tags.Sort();
            return this.Ok(tags);
        }
        return this.Ok(Array.Empty<string>());
    }

        private async Task<List<Recipe>> DeserializeAllRecipes(CancellationToken ct)
    {
        string[] jsonFilePaths = this.fileSystem.Directory.GetFiles(this.dataDirectoryPath, "*.json");
        var recipes = new List<Recipe>();
        foreach (var filePath in jsonFilePaths)
        {
            using Stream stream = this.fileSystem.File.OpenRead(filePath);
            var recipe = await JsonSerializer.DeserializeAsync<Recipe>(stream, this.jsonOptions, ct);
            recipes.Add(recipe!);
        }
        return recipes.OrderByDescending(recipe => recipe.Date)
                      .Where(recipe => recipe.Date <= DateTime.Now)
                      .Where(recipe => !recipe.IsDraft)
                      .ToList();
    }



}