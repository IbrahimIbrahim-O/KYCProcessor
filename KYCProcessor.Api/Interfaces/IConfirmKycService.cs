using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Response;

namespace KYCProcessor.Api.Interfaces
{
    public interface IConfirmKycService
    {
        Task<ConfirmKycResponse> ConfirmKycFormAsync(ConfirmKycFormRequest request);
    }
}
