using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace KYCProcessor.Api.Services
{
    public class RejectKycService : IRejectKycService
    {
        private readonly IDapperService _dapperService;
        private readonly ILogger<RejectKycService> _logger;

        public RejectKycService(IDapperService dapperService, ILogger<RejectKycService> logger)
        {
            _dapperService = dapperService;
            _logger = logger;
        }

        public async Task<RejectKycResponse> RejectKycFormAsync(RejectKycFormRequest request)
        {
            var response = new RejectKycResponse();
            var validationResults = new List<ValidationResult>();

            try
            {
                _logger.LogInformation("Received KYC rejection request for PhoneNumber: {PhoneNumber}", request.PhoneNumber);

                // Validate request
                if (request == null)
                {
                    _logger.LogError("Received a null request for KYC rejection");
                    response.Success = false;
                    response.Message = "Request cannot be null";
                    return response;
                }

                var context = new ValidationContext(request);
                bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

                if (!isValid)
                {
                    _logger.LogWarning(message: "Validation failed for KYC rejection for PhoneNumber: {PhoneNumber}. Errors: {ValidationErrors}", request.PhoneNumber, validationResults);
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.ValidationErrors = validationResults;
                    return response;
                }

                // Check if the KYC form with the given phone number exists and is in pending status
                var param = new DynamicParameters();
                var query = $"SELECT * FROM KycForms WHERE PhoneNumber = '{request.PhoneNumber}' AND KycStatus = '{(int)KycStatus.Pending}'";
                _logger.LogInformation(query);


                var kycRequestToReject = _dapperService.Get<KycForm>(query, param, CommandType.Text);

                if (kycRequestToReject == null)
                {
                    _logger.LogWarning("No pending KYC form found for PhoneNumber: {PhoneNumber}.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "KYC form does not exist or is not in pending status.";
                    return response;
                }

                // Mark the KYC form as rejected
                param = new DynamicParameters();
                var updateQuery = $"UPDATE KycForms SET KycStatus = '{(int)KycStatus.Rejected}' WHERE Id = '{kycRequestToReject.Id}'";
                
                _dapperService.Get<int>(updateQuery, param, CommandType.Text);

                _logger.LogInformation("KYC form for PhoneNumber: {PhoneNumber} has been rejected.", request.PhoneNumber);

                response.Success = true;
                response.Message = "KYC form has been rejected.";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the KYC rejection request.");
                response.Success = false;
                response.Message = "An error occurred during KYC rejection.";
                return response;
            }
        }
    }
}
