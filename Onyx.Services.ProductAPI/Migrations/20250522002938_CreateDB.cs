using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Onyx.Services.ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Colour = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "CategoryName", "Colour", "Description", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Appetizer", "Golden Brown", "A crispy and spicy Indian snack filled with mashed potatoes, peas, and aromatic spices.", "Samosa", 15m },
                    { 2, "Appetizer", "Orange", "Marinated paneer cubes grilled to perfection with capsicum and onions, served with mint chutney.", "Paneer Tikka", 13.99m },
                    { 3, "Dessert", "Yellow", "An Indian-style sweet pie with a hint of cardamom, nuts, and saffron, served warm.", "Sweet Pie", 10.99m },
                    { 4, "Entree", "Reddish Brown", "A famous Mumbai street food — spicy mashed vegetables served with buttered pav (bread rolls).", "Pav Bhaji", 15m },
                    { 5, "Entree", "Light Brown", "A thin, crispy rice crepe stuffed with spiced mashed potatoes, served with coconut chutney and sambar.", "Masala Dosa", 12.50m },
                    { 6, "Dessert", "Dark Brown", "Soft deep-fried milk balls soaked in sugar syrup, flavored with rose water and cardamom.", "Gulab Jamun", 7.99m },
                    { 7, "Entree", "Mixed Yellow/Brown", "Aromatic basmati rice cooked with marinated chicken, saffron, and traditional Hyderabadi spices.", "Chicken Biryani", 18.99m },
                    { 8, "Dessert", "White", "Soft and spongy cheese balls soaked in light sugar syrup, a classic Bengali dessert.", "Rasgulla", 6.50m },
                    { 9, "Entree", "Brown", "A North Indian dish with spicy chickpeas (chole) served with deep-fried bread (bhature).", "Chole Bhature", 14.00m },
                    { 10, "Appetizer", "Golden Brown", "Crispy potato patties seasoned with Indian spices, often served with tamarind and mint chutneys.", "Aloo Tikki", 8.00m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
