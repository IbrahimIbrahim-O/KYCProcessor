using KYCProcessor.Api.Dapper;
using KYCProcessor.Data;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Response;

namespace KYCProcessor.Api.Interfaces
{
    public interface ISignUpService
    {
        Task<SignUpResponse> HandleSignUp(SignUpRequest request);
    }
}
