using Microsoft.AspNetCore.Mvc;
using MyEcommerceAPI.Models;
using MyEcommerceAPI.Services;

namespace MyEcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(string id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Product product)
        {
            await _productService.CreateAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }


        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] Product updatedProduct)
        {
            var existing = await _productService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            updatedProduct.Id = existing.Id;
            updatedProduct.CreatedAt = existing.CreatedAt; 
            updatedProduct.UpdatedAt = DateTime.UtcNow;

            await _productService.UpdateAsync(id, updatedProduct);
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            await _productService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<Product>>> Search([FromQuery] string query, [FromQuery] string? category = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }
                var results = await _productService.SearchAsync(query, category);
                if (!results.Any())
                {
                    return Ok(new { message = "No products found matching your search criteria", results });
                }
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching products", error = ex.Message });
            }
        }

        [HttpGet("autocomplete")]
        public async Task<ActionResult<List<string>>> Autocomplete([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty.");

            var suggestions = await _productService.AutocompleteAsync(query);
            return Ok(suggestions);
        }

    }
}