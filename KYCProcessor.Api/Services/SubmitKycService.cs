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
   
    public class SubmitKycService : ISubmitKycService
    {
        private readonly IDapperService _dapperService;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public SubmitKycService(IDapperService dapperService, ILogger<SubmitKycService> logger, IConfiguration config)
        {
            _dapperService = dapperService;
            _logger = logger;
            _config = config;
        }

        public async Task<SubmitKycResponse> SubmitKycFormAsync(SubmitKycFormRequest request)
        {
            var response = new SubmitKycResponse();

            try
            {
                _logger.LogInformation("Received KYC form submission request for PhoneNumber: {PhoneNumber}", request.PhoneNumber);

                // Validate the request
                var validationResults = new List<ValidationResult>();
                var context = new ValidationContext(request);
                bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

                if (!isValid)
                {
                    _logger.LogWarning("Validation failed for KYC form submission for PhoneNumber: {PhoneNumber}. Errors: {ValidationErrors}", request.PhoneNumber, validationResults);
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.ValidationErrors = validationResults;
                    return response;
                }

                if (request == null)
                {
                    _logger.LogError("Received a null request for KYC form submission");
                    response.Success = false;
                    response.Message = "Request cannot be null";
                    return response;
                }

                // Check if a pending KYC form exists for this phone number
                var param = new DynamicParameters();
                var existingPendingKycQuery = $"SELECT * FROM KycForms WHERE PhoneNumber = '{request.PhoneNumber}' AND KycStatus = {(int)KycStatus.Pending}";
                var existingPendingKyc =  _dapperService.Get<KycForm>(existingPendingKycQuery, param , CommandType.Text);

                if (existingPendingKyc != null)
                {
                    _logger.LogWarning("KYC form for PhoneNumber: {PhoneNumber} is already pending.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "You currently have a pending KYC request. Our team is reviewing your information and will respond shortly.";
                    return response;
                }

                param = new DynamicParameters();
                // Check if KYC has already been confirmed for this phone number
                var existingConfirmedKycQuery = $"SELECT * FROM KycForms WHERE PhoneNumber = '{request.PhoneNumber}' AND KycStatus = {(int)KycStatus.Confirmed}";
                var existingConfirmedKyc =  _dapperService.Get<KycForm>(existingConfirmedKycQuery, param , CommandType.Text);

                if (existingConfirmedKyc != null)
                {
                    _logger.LogInformation("KYC form for PhoneNumber: {PhoneNumber} has already been confirmed.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "Your KYC form has already been confirmed.";
                    return response;
                }

                // Insert the new KYC form with Pending status
                var newKycForm = new KycForm
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = request.PhoneNumber,
                    Name = request.FirstName,
                    KycStatus = KycStatus.Pending,  // Set the status to Pending
                    CreatedAt = DateTime.UtcNow
                };

                param = new DynamicParameters();
                var insertKycFormQuery = $"INSERT INTO KycForms (Id, PhoneNumber, Name, KycStatus, CreatedAt) VALUES" +
                                $" ('{newKycForm.Id}', '{newKycForm.PhoneNumber}', '{newKycForm.Name}', {(int)newKycForm.KycStatus}, " +
                                $"'{newKycForm.CreatedAt}')";

                _logger.LogInformation(insertKycFormQuery);
                _dapperService.Get<int>(insertKycFormQuery, param, CommandType.Text);

                // confirm it was submitted
                param = new DynamicParameters();
                var kycFormConfirmQuery = $"select * from KycForms where id ='{newKycForm.Id}'";
                var kycForm = _dapperService.Get<User>(kycFormConfirmQuery, param, CommandType.Text);

                if (kycForm == null)
                {
                    _logger.LogError("Failed to submit KYC form for PhoneNumber: {PhoneNumber}.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "Failed to submit KYC form.";
                }

                _logger.LogInformation("Successfully submitted KYC form for PhoneNumber: {PhoneNumber}. KYC Form ID: {KycFormId}", request.PhoneNumber, newKycForm.Id);
                response.Success = true;
                response.Message = "KYC form submitted successfully. Our team will review it and respond within 24 hours.";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the KYC form submission request.");
                response.Success = false;
                response.Message = "An error occurred during KYC form submission.";
                return response;
            }
        }

    }
}
