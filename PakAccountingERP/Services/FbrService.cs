using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using System.Net.Http.Headers;
using System.Text;

namespace PakAccountingERP.Services;

public class FbrSettings
{
    public string DefaultPostUrl { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
}

public class FbrService : IFbrService
{
    private readonly HttpClient _httpClient;
    private readonly FbrSettings _settings;
    private readonly IAuditService _auditService;
    private readonly ILogger<FbrService> _logger;

    public FbrService(HttpClient httpClient, IOptions<FbrSettings> settings,
        IAuditService auditService, ILogger<FbrService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<FbrSubmissionResult> SubmitInvoiceAsync(SalesInvoice invoice, Company company)
    {
        var payload = BuildFbrPayload(invoice, company);
        var json = JsonConvert.SerializeObject(payload);
        var url = string.IsNullOrWhiteSpace(company.FbrHttpPostUrl) ? _settings.DefaultPostUrl : company.FbrHttpPostUrl;

        for (var attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                if (!string.IsNullOrWhiteSpace(company.ApiToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", company.ApiToken);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var fbrNumber = ExtractFbrInvoiceNumber(responseBody);
                    var qrData = $"FBR|{fbrNumber}|{invoice.InvoiceNumber}|{invoice.NetTotal}|{invoice.InvoiceDate:yyyy-MM-dd}";

                    await _auditService.LogAsync("FBR Invoice Submitted", "SalesInvoices", invoice.Id.ToString(),
                        null, responseBody);

                    return new FbrSubmissionResult
                    {
                        Success = true,
                        FbrInvoiceNumber = fbrNumber,
                        ResponseJson = responseBody,
                        QrCodeData = qrData
                    };
                }

                _logger.LogWarning("FBR submission attempt {Attempt} failed: {Status} - {Body}",
                    attempt, response.StatusCode, responseBody);

                if (attempt < _settings.MaxRetryAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds * attempt));
                else
                {
                    await _auditService.LogAsync("FBR Invoice Failed", "SalesInvoices", invoice.Id.ToString(),
                        null, responseBody);
                    return new FbrSubmissionResult
                    {
                        Success = false,
                        ResponseJson = responseBody,
                        ErrorMessage = $"FBR API returned {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FBR submission attempt {Attempt} exception", attempt);
                if (attempt == _settings.MaxRetryAttempts)
                {
                    await _auditService.LogAsync("FBR Invoice Error", "SalesInvoices", invoice.Id.ToString(),
                        null, ex.Message);
                    return new FbrSubmissionResult { Success = false, ErrorMessage = ex.Message };
                }
                await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds * attempt));
            }
        }

        return new FbrSubmissionResult { Success = false, ErrorMessage = "Max retry attempts exceeded" };
    }

    private static object BuildFbrPayload(SalesInvoice invoice, Company company)
    {
        return new
        {
            invoiceType = invoice.InvoiceType.ToString(),
            invoiceDate = invoice.InvoiceDate.ToString("yyyy-MM-dd"),
            sellerNTNCNIC = company.NTN,
            sellerBusinessName = company.CompanyName,
            sellerProvince = company.Province?.Name,
            sellerAddress = company.Address,
            buyerNTNCNIC = invoice.BuyerNTN ?? invoice.BuyerCNIC,
            buyerBusinessName = invoice.Customer?.BuyerName,
            buyerProvince = invoice.Province?.Name,
            buyerAddress = invoice.BuyerAddress,
            invoiceRefNo = invoice.InvoiceNumber,
            scenarioId = "SN001",
            items = invoice.Items.Select(i => new
            {
                hsCode = i.HSCode,
                productDescription = i.ProductDescription,
                rate = i.TaxRate,
                uoM = i.UnitOfMeasure?.Name,
                quantity = i.Quantity,
                totalValues = i.LineTotal,
                valueSalesExcludingST = i.LineTotal - i.TaxAmount,
                salesTaxApplicable = i.TaxAmount,
                furtherTax = 0,
                extraTax = 0,
                fedPayable = 0,
                discount = i.Discount,
                saleType = invoice.SalesType.ToString(),
                sroScheduleNo = "",
                sroItemSerialNo = ""
            })
        };
    }

    private static string? ExtractFbrInvoiceNumber(string responseBody)
    {
        try
        {
            dynamic? result = JsonConvert.DeserializeObject(responseBody);
            return result?.invoiceNumber?.ToString() ?? result?.fbrInvoiceNo?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
