using Restaurant.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> LoginAsync(string username, string password);
    }
}
