using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Response;

namespace KYCProcessor.Api.Interfaces
{
    public interface ISubmitKycService
    {
        Task<SubmitKycResponse> SubmitKycFormAsync(SubmitKycFormRequest request);
    }
}
