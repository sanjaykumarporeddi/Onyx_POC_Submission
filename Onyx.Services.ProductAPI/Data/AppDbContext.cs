using Onyx.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Onyx.Services.ProductAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().HasData(
                // Starters (Appetizers)
                new Product
                {
                    ProductId = 1,
                    Name = "Scotch Egg",
                    Price = 4.50m,
                    Description = "A hard-boiled egg wrapped in sausage meat, coated in breadcrumbs and baked or deep-fried.",
                    CategoryName = "Starter",
                    Colour = "Golden Brown"
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Prawn Cocktail",
                    Price = 5.95m,
                    Description = "Cooked prawns in a Marie Rose sauce, served in a glass with lettuce.",
                    CategoryName = "Starter",
                    Colour = "Pink"
                },
                new Product
                {
                    ProductId = 3,
                    Name = "Soup of the Day",
                    Price = 4.00m,
                    Description = "Freshly made soup, served with a crusty bread roll.",
                    CategoryName = "Starter",
                    Colour = "Varies" // Or a specific colour like "Orange" for tomato soup
                },

                // Mains (Entrees)
                new Product
                {
                    ProductId = 4,
                    Name = "Fish and Chips",
                    Price = 12.50m,
                    Description = "Battered cod or haddock, served with thick-cut chips, mushy peas, and tartar sauce.",
                    CategoryName = "Main",
                    Colour = "Golden"
                },
                new Product
                {
                    ProductId = 5,
                    Name = "Shepherd's Pie",
                    Price = 11.00m,
                    Description = "Minced lamb cooked in gravy with vegetables, topped with a layer of mashed potato.",
                    CategoryName = "Main",
                    Colour = "Brown"
                },
                new Product
                {
                    ProductId = 6,
                    Name = "Bangers and Mash",
                    Price = 10.50m,
                    Description = "Sausages served with mashed potatoes and onion gravy.",
                    CategoryName = "Main",
                    Colour = "Brown"
                },
                new Product
                {
                    ProductId = 7,
                    Name = "Sunday Roast (Beef)",
                    Price = 15.95m,
                    Description = "Roast beef served with Yorkshire pudding, roast potatoes, seasonal vegetables, and gravy.",
                    CategoryName = "Main",
                    Colour = "Brown/Mixed"
                },

                // Desserts
                new Product
                {
                    ProductId = 8,
                    Name = "Sticky Toffee Pudding",
                    Price = 6.50m,
                    Description = "A very moist sponge cake, made with finely chopped dates, covered in a toffee sauce and often served with vanilla custard or ice cream.",
                    CategoryName = "Dessert",
                    Colour = "Dark Brown"
                },
                new Product
                {
                    ProductId = 9,
                    Name = "Eton Mess",
                    Price = 5.75m,
                    Description = "A traditional English dessert consisting of a mixture of strawberries, broken meringue, and whipped double cream.",
                    CategoryName = "Dessert",
                    Colour = "Pink/White"
                },
                new Product
                {
                    ProductId = 10,
                    Name = "Apple Crumble",
                    Price = 6.00m,
                    Description = "Cooked apples topped with a crumbly mixture of flour, butter, and sugar, often served with custard.",
                    CategoryName = "Dessert",
                    Colour = "Golden Brown"
                }
            );
        }
    }
}