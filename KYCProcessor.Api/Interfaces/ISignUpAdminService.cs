using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Response;

namespace KYCProcessor.Api.Interfaces
{
    public interface ISignUpAdminService
    {
        Task<SignUpAdminResponse> HandleAdminSignUp(SignUpAdminRequest request);
    }
}
