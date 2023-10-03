using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using TeamsMeetingCreationPoC.controller;
using System.Net.Http.Headers;
using TeamsMeetingCreationPoC.Model;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json");
var config = builder.Build();

string clientId = config["AZURE_CLIENT_ID"];
string tenantId = config["AZURE_TENANT_ID"];
string clientSecret = config["AZURE_CLIENT_SECRET"];
string userPrincipalName = "markus@mmoellermvp.onmicrosoft.com";
string dummyAttendee = "cclausen@mmoellermvp.onmicrosoft.com";

string customerName = "Contoso";
string customerEmail = "BenBenson@contoso.com";
string customerPhone = "+491515445556";
string customerId = "47110815";

Customer customer = new Customer()
{
  Id = customerId,
  Name = customerName,
  Email = customerEmail,
  Phone = customerPhone
};

GraphController graphController = new GraphController(tenantId, clientId, clientSecret);

string meetingSubject = "Test Meeting with App/Tab 5";
//OnlineMeeting om = new OnlineMeeting
//{
//    Subject = meetingSubject,
//    StartDateTime = DateTime.Now,
//    EndDateTime = DateTime.Now.AddHours(1)
//};
string userID = await graphController.GetUserId(userPrincipalName);
string joinUrl = await graphController.CreateTeamsMeeting(userID, userPrincipalName, dummyAttendee, meetingSubject);

// string joinUrl = "https://teams.microsoft.com/l/meetup-join/19%3ameeting_NTFhNDc2NjktMmJkMi00ZGMxLWJkNDYtNDNiZWQ3YTI2NGRh%40thread.v2/0?context=%7b%22Tid%22%3a%227e77d071-ed08-468a-bc75-e8254ba77a21%22%2c%22Oid%22%3a%226c76948a-7520-4c33-bf37-b1a39b78859f%22%7d";
// !!! https://learn.microsoft.com/en-us/graph/cloud-communication-online-meeting-application-access-policy

// HttpClientController clientController = new HttpClientController(accessToken);
// string onlineMeetingResult = await clientController.GetOnlineMeeting(userID, joinUrl);
// Console.WriteLine($"OnlineMeeting /n {onlineMeetingResult}");
string chatId = await graphController.GetMeetingChatId(userID, joinUrl);

string appId = await graphController.GetAppId();
if (appId != "")
{
  bool appInstalled = await graphController.InstallAppInChat(appId, chatId);
  if (appInstalled)
  {
    await graphController.InstallTabInChat(appId, chatId);
  }
}
AzureController azrCtrl = new AzureController(config);
azrCtrl.storeConfigValue($"TEAMSMEETINGSERVICECALL:{chatId}:CUSTOMERNAME", customerName);
azrCtrl.storeConfigValue($"TEAMSMEETINGSERVICECALL:{chatId}:CUSTOMERPHONE", customerPhone);
azrCtrl.storeConfigValue($"TEAMSMEETINGSERVICECALL:{chatId}:CUSTOMEREMAIL", customerEmail);
azrCtrl.storeConfigValue($"TEAMSMEETINGSERVICECALL:{chatId}:CUSTOMERID", customerId);

AzureTableController azureTableController = new AzureTableController();
azureTableController.CreateCustomer(chatId, customer);
Console.ReadLine();