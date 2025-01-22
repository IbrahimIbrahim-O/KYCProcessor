using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Response;

namespace KYCProcessor.Api.Interfaces
{
    public interface ILoginService
    {
        Task<LoginResponse> LoginAsync(LoginUserRequest request);
    }
}
