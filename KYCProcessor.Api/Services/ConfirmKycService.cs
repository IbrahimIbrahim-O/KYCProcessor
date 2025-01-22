using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using System.Data;
using Dapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KYCProcessor.Api.Services
{
    public class ConfirmKycService : IConfirmKycService
    {
        private readonly IDapperService _dapperService;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        public ConfirmKycService(IDapperService dapperService, ILogger<ConfirmKycService> logger, IConfiguration config)
        {
            _dapperService = dapperService;
            _logger = logger;
            _config = config;
        }

        public async Task<ConfirmKycResponse> ConfirmKycFormAsync(ConfirmKycFormRequest request)
        {
            using var connection = new SqlConnection(_config["ConnectionStrings:DefaultConnection"]);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var response = new ConfirmKycResponse();

            try
            {
                _logger.LogInformation("Received KYC confirmation request for PhoneNumber: {PhoneNumber}", request.PhoneNumber);

                if (request == null)
                {
                    _logger.LogError("Received a null request for KYC confirmation");
                    response.Success = false;
                    response.Message = "Request cannot be null";
                    return response;
                }

                // Check if KYC is already confirmed
                var param = new DynamicParameters();
                var existingConfirmedKycQuery = $"SELECT * FROM KycForms WHERE PhoneNumber='{request.PhoneNumber}' AND KycStatus={(int)KycStatus.Confirmed}";


                var existingConfirmedKyc = _dapperService.Get<KycForm>(existingConfirmedKycQuery, param, CommandType.Text);
                if (existingConfirmedKyc != null)
                {
                    _logger.LogWarning("KYC form for PhoneNumber: {PhoneNumber} has already been confirmed.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "This customer already has a confirmed KYC";
                    return response;
                }

                param = new DynamicParameters();

                // Check if there's a pending KYC form
                var existingPendingKycQuery = $"SELECT * FROM KycForms WHERE PhoneNumber='{request.PhoneNumber}' AND KycStatus ={(int)KycStatus.Pending}";
                _logger.LogInformation(existingPendingKycQuery);

                var existingPendingKyc = _dapperService.Get<KycForm>(existingPendingKycQuery, param, CommandType.Text);

                if (existingPendingKyc == null)
                {
                    _logger.LogWarning("No pending KYC form found for PhoneNumber: {PhoneNumber}.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "No pending KYC form found";
                    return response;
                }

                param = new DynamicParameters();
                // Update KYC status to Confirmed
                var updateKycQuery = $"UPDATE KycForms SET KycStatus = {(int)KycStatus.Confirmed} WHERE PhoneNumber = {request.PhoneNumber} AND KycStatus = {(int)KycStatus.Pending}";
                _logger.LogInformation(updateKycQuery);

                var rowsAffected = _dapperService.Get<int>(updateKycQuery, param, CommandType.Text);

                // Confirm that kyc status was changed to confirmed
                var kycConfirmQuery = $"SELECT * FROM KycForms WHERE PhoneNumber = {request.PhoneNumber} AND KycStatus = {(int)KycStatus.Confirmed}";
                var newConfirmedKyc = _dapperService.Get<KycForm>(kycConfirmQuery, param, CommandType.Text);
                _logger.LogInformation(updateKycQuery);

                if (newConfirmedKyc == null)
                {
                    _logger.LogError("Failed to update KYC status for PhoneNumber: {PhoneNumber}", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "Failed to confirm KYC";
                    return response;
                }

                param = new DynamicParameters();
                // Check if the user has already received the credit
                var checkUserCreditQuery = $"SELECT * FROM UserCredits WHERE PhoneNumber = {request.PhoneNumber} AND CreditStatus = {(int)CreditStatus.Credited}";
                _logger.LogInformation(checkUserCreditQuery);

                var existingUserCredit = _dapperService.Get<UserCredit>(checkUserCreditQuery, param, CommandType.Text);

                if (existingUserCredit != null)
                {
                    _logger.LogWarning("Customer with PhoneNumber: {PhoneNumber} has already received the 200 Naira credit.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "Customer has already received credit";
                    transaction.Rollback();
                    return response;
                }

                param = new DynamicParameters();

                var addCreditQyeryId = Guid.NewGuid();

                // Add the 200 Naira credit
                var addCreditQuery = $"INSERT INTO UserCredits (Id, PhoneNumber, Amount, CreditStatus, CreatedAt) VALUES" +
                    $" ('{addCreditQyeryId}', '{request.PhoneNumber}', {200}, {(int)CreditStatus.Credited}, '{DateTime.Now}')";
                _logger.LogInformation(addCreditQuery);

                _dapperService.Get<int>(addCreditQuery, param, CommandType.Text);

                _logger.LogInformation("200 Naira credit successfully added for PhoneNumber: {PhoneNumber}", request.PhoneNumber);
                transaction.Commit();
                response.Success = true;
                response.Message = "KYC confirmed and 200 Naira credit issued";

                return response;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the KYC confirmation request.");
                transaction.Rollback();
                response.Success = false;
                response.Message = "An error occurred during KYC confirmation";
                return response;
            }
        }
    }
}
