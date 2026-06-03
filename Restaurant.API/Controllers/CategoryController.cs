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
    [Authorize(Roles = "SystemAdmin,SuperAdmin,Admin")] // Only privileged roles can manage Categories
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly Cloudinary _cloudinary;

        public CategoryController(ICategoryRepository categoryRepository, Cloudinary cloudinary)
        {
            _categoryRepository = categoryRepository;
            _cloudinary = cloudinary;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _categoryRepository.GetAllAsync();
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _categoryRepository.GetByIdAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] Category category, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(imageFile.FileName, imageFile.OpenReadStream()),
                        Folder = "restaurant/categories"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    category.ImageUrl = uploadResult.SecureUrl.ToString();
                }

                var response = await _categoryRepository.AddAsync(category);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }


        [HttpPut("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] Category category, IFormFile? imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(imageFile.FileName, imageFile.OpenReadStream()),
                        Folder = "restaurant/categories"
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    category.ImageUrl = uploadResult.SecureUrl.ToString();
                }

                var response = await _categoryRepository.UpdateAsync(category);
                return StatusCode(response.StatusCode, response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    ex.InnerException?.Message ?? ex.Message);
            }
        }


        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _categoryRepository.DeleteAsync(id);
            return StatusCode(response.StatusCode, response);
        }
    }
}