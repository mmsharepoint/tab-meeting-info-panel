using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using TeamsMeetingServiceCall.controller;
using TeamsMeetingServiceCall.Models;

namespace TeamsMeetingServiceCall.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphController : ControllerBase
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<GraphController> _logger;
        public GraphController(ITokenAcquisition tokenAcquisition, GraphServiceClient graphClient, ILogger<GraphController> logger)
        {
            _tokenAcquisition = tokenAcquisition;
            _graphClient = graphClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<string>> Get(string meetingId)
        {
            _logger.LogInformation($"MeetingID: {meetingId}");
            string chatId = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(meetingId));
            _logger.LogInformation($"MeetingID decoded: {chatId.Replace("0#", "").Replace("#0", "")}");
            string plainChatId = chatId.Replace("0#", "").Replace("#0", "");
            Chat chat = await this._graphClient.Chats[plainChatId].Request().GetAsync();

            _logger.LogInformation($"OrganizerID: {chat.OnlineMeetingInfo.Organizer.Id}");
            _logger.LogInformation($"JoinWebUrl: {chat.OnlineMeetingInfo.JoinWebUrl}");
            CustomerData result = await this.GetCustomMeetingData(plainChatId);
            return Ok(result);
        }

        private async Task<string> GetAccessToken()
        {
            _logger.LogInformation($"Authenticated user: {User.GetDisplayName()}");

            try
            {
                // TEMPORARY
                // Get a Graph token via OBO flow
                var token = await _tokenAcquisition
                    .GetAccessTokenForUserAsync(new[]{
                        "Files.ReadWrite", "Sites.ReadWrite.All" });

                // Log the token
                _logger.LogInformation($"Access token for Graph: {token}");
                return token;
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                _logger.LogError(ex, "Consent required");
                // This exception indicates consent is required.
                // Return a 403 with "consent_required" in the body
                // to signal to the tab it needs to prompt for consent
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred");
                return "";
            }
        }

        private async Task<CustomerData> GetCustomMeetingData(string meetingId)
        {
            AzureController azrCtrl = new AzureController("Endpoint=https://mmoteamsconfiguration.azconfig.io;Id=/nk1-l9-s0:JJRg85Y6Y+GzQ1rRLdzf;Secret=wX+jBL/p0WwB/3Z8SGVP8rMsQ8t1DcZC+te5wK84nuw="); // ToDo!!         
            string customerName = azrCtrl.GetConfigurationValue($"TEAMSMEETINGSERVICECALL:{meetingId}:CUSTOMERNAME") ?? "No customername";
            string customerPhone = azrCtrl.GetConfigurationValue($"TEAMSMEETINGSERVICECALL:{meetingId}:CUSTOMERPHONE") ?? "No customerphone";
            string customerEmail = azrCtrl.GetConfigurationValue($"TEAMSMEETINGSERVICECALL:{meetingId}:CUSTOMEREMAIL") ?? "No customeremail";
            string customerId = azrCtrl.GetConfigurationValue($"TEAMSMEETINGSERVICECALL:{meetingId}:CUSTOMERID") ?? "No customerid";

            CustomerData customerData = new CustomerData
            {
                Name = customerName,
                Phone = customerPhone,
                Email = customerEmail,
                Id = customerId
            };
            return customerData;
        }
    }
}
