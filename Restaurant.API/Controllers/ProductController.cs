using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using System.Net;

namespace Restaurant.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage Products
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _repository;
        private readonly Cloudinary _cloudinary;
        public ProductsController(IProductRepository repository, Cloudinary cloudinary)
        {
            _repository = repository;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var response = await _repository.GetAllAsync();
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Product?>> GetById(int id)
        {
            try
            {
                var response = await _repository.GetByIdAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromForm] Product product, IFormFile? imageFile)
        {
            try
            {
                // Check if ImageUrl contains a file instead of URL text
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(imageFile.FileName, imageFile.OpenReadStream()),
                        Folder = "restaurant/products"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    // Replace file object with URL string
                    product.ImageUrl = uploadResult.SecureUrl.ToString();
                }

                var response = await _repository.AddAsync(product);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromForm] Product entity, IFormFile? imageFile)
        {
            try
            {
                if (id != entity.ProductId)
                    return BadRequest("Product ID mismatch.");

                // Upload new image if one is provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(imageFile.FileName, imageFile.OpenReadStream()),
                        Folder = "restaurant/products"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    entity.ImageUrl = uploadResult.SecureUrl.ToString();
                }

                var response = await _repository.UpdateAsync(entity);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }


        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var response = await _repository.DeleteAsync(id);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
