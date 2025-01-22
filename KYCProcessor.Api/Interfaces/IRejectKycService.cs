using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Response;

namespace KYCProcessor.Api.Interfaces
{
    public interface IRejectKycService
    {
        Task<RejectKycResponse> RejectKycFormAsync(RejectKycFormRequest request);

    }
}
