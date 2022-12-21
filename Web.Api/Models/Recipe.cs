namespace ModerationKitchen.Web.Api.Models;

public class Recipe {
    private IReadOnlyList<RecipeComment> comments = new List<RecipeComment>();

    public string Slug {get;set;}
    public bool IsDraft {get;set;}
    public string Title {get;set;}
    public string Author {get;set;}
    public DateTime Date {get;set;}
    public string Intro {get;set;}
    public string HeroImage {get;set;}
    public string Body {get;set;}
    public RecipeRating? Rating {get; private set;}
    public string PrepTime {get;set;}
    public string CookTime {get;set;}
    public string QuantitySizeMade {get;set;}
    public IReadOnlyList<string> Ingredients {get;set;}
    public IReadOnlyList<string> Method {get;set;}
    public IReadOnlyList<string> Tags {get;set;}
    public IReadOnlyList<RecipeComment> Comments {get => this.comments; set{ 
        this.comments = value;
        this.UpdateRating();
    }}

    private void UpdateRating() {
        if (this.Comments.Count == 0)
        {
            this.Rating = null;
            return;
        }
        var rating = new RecipeRating(this.Comments.Average( comment => comment.Rating), this.Comments.Count);
        this.Rating = rating;
    }
}