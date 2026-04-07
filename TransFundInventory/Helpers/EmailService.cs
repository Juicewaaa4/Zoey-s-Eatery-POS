using System.Net.Http;
using System.Text;
using System.Text.Json;
using TransFundInventory.Data;
using TransFundInventory.Models;

namespace TransFundInventory.Helpers
{
    public static class EmailService
    {
        private static readonly HttpClient _httpClient = new();

        /// <summary>
        /// Send a login notification email to the owner (runs in background)
        /// </summary>
        public static void SendLoginNotification(string username, string fullName, string role, DateTime loginTime)
        {
            Task.Run(async () =>
            {
                try
                {
                    var repo = new EmailSettingsRepository();
                    var settings = repo.GetSettings();

                    if (settings == null || !settings.IsEnabled || !settings.NotifyOnLogin)
                        return;

                    var subject = $"🔔 Login Alert — {role} {fullName}";
                    var body = BuildLoginEmailBody(username, fullName, role, loginTime);

                    await SendViaResendAsync(settings.ResendApiKey, settings.OwnerEmail, settings.OwnerName, subject, body);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Email send failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Send a low stock alert email to the owner (runs in background)
        /// </summary>
        public static void SendLowStockNotification(int lowStockCount, string section)
        {
            Task.Run(async () =>
            {
                try
                {
                    var repo = new EmailSettingsRepository();
                    var settings = repo.GetSettings();

                    if (settings == null || !settings.IsEnabled || !settings.NotifyOnLowStock)
                        return;

                    var subject = $"⚠️ Low Stock Alert — {lowStockCount} item(s) in {section}";
                    var body = BuildLowStockEmailBody(lowStockCount, section);

                    await SendViaResendAsync(settings.ResendApiKey, settings.OwnerEmail, settings.OwnerName, subject, body);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Email send failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Send a test email to verify settings are working
        /// </summary>
        public static async Task<(bool Success, string Message)> SendTestEmailAsync(string apiKey, string ownerEmail, string ownerName)
        {
            try
            {
                var subject = "✅ Test Email — Zoey's System";
                var body = @$"
                <html>
                <body style='font-family: Segoe UI, Arial, sans-serif; background-color: #f5f7fa; padding: 30px;'>
                    <div style='max-width: 500px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <div style='background: linear-gradient(135deg, #1b5e20, #2e7d32); padding: 25px; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 20px;'>✅ Test Successful!</h1>
                        </div>
                        <div style='padding: 25px;'>
                            <p style='color: #333; font-size: 15px;'>
                                Kumusta, <strong>{ownerName}</strong>! 👋
                            </p>
                            <p style='color: #555; font-size: 14px;'>
                                This is a test email from <strong>Zoey's Billiard House</strong> system.
                                If you received this, your email notifications are working correctly!
                            </p>
                            <div style='background: #e8f5e9; padding: 15px; border-radius: 8px; margin-top: 15px;'>
                                <p style='color: #1b5e20; margin: 0; font-size: 13px;'>
                                    📬 Receiver: {ownerEmail}<br/>
                                    🕐 Time: {DateTime.Now:MMMM dd, yyyy — hh:mm tt}
                                </p>
                            </div>
                        </div>
                        <div style='background: #f5f5f5; padding: 12px; text-align: center;'>
                            <p style='color: #999; font-size: 11px; margin: 0;'>Zoey's Billiard House — Paltao, Pulilan, Bulacan</p>
                        </div>
                    </div>
                </body>
                </html>";

                await SendViaResendAsync(apiKey, ownerEmail, ownerName, subject, body);
                return (true, "Test email sent successfully! Check your inbox (or spam folder).");
            }
            catch (Exception ex)
            {
                return (false, $"Failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send email using Resend API — just a simple HTTP POST, no packages needed!
        /// </summary>
        private static async Task SendViaResendAsync(string apiKey, string toEmail, string toName, string subject, string htmlBody)
        {
            var payload = new
            {
                from = "Zoey's System <onboarding@resend.dev>",
                to = new[] { toEmail },
                subject = subject,
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Resend API error ({response.StatusCode}): {errorBody}");
            }
        }

        private static string BuildLoginEmailBody(string username, string fullName, string role, DateTime loginTime)
        {
            return @$"
            <html>
            <body style='font-family: Segoe UI, Arial, sans-serif; background-color: #f5f7fa; padding: 30px;'>
                <div style='max-width: 500px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                    <div style='background: linear-gradient(135deg, #1b5e20, #2e7d32); padding: 25px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 20px;'>🔔 Login Alert</h1>
                        <p style='color: #a5d6a7; margin: 5px 0 0; font-size: 13px;'>Zoey's Billiard House</p>
                    </div>
                    <div style='padding: 25px;'>
                        <p style='color: #333; font-size: 15px;'>
                            This is an automated notification to inform you of a recent login to the Point of Sale system.
                        </p>
                        <div style='background: #fff3e0; padding: 18px; border-radius: 8px; border-left: 4px solid #ff9800;'>
                            <p style='margin: 0; color: #333; font-size: 14px;'>
                                👤 <strong>Name:</strong> {fullName} <span style='background: #e0f2f1; padding: 2px 6px; border-radius: 4px; font-size: 11px; margin-left: 5px;'>{role}</span><br/>
                                🔑 <strong>Username:</strong> {username}<br/>
                                📅 <strong>Date:</strong> {loginTime:MMMM dd, yyyy}<br/>
                                🕐 <strong>Time:</strong> {loginTime:hh:mm:ss tt}
                            </p>
                        </div>
                        <p style='color: #888; font-size: 13px; margin-top: 15px;'>
                            If you recognize this activity, no further action is required. If this login is unauthorized, please investigate immediately.
                        </p>
                        <hr style='border: none; border-top: 1px dashed #eee; margin: 15px 0;'/>
                        <p style='color: #bbb; font-size: 11px; margin: 0; font-style: italic;'>
                            System Note: Your current email limit is 100 messages per day. To conserve this quota, you may disable ""Login Alerts"" in the System Settings at any time.
                        </p>
                    </div>
                    <div style='background: #f5f5f5; padding: 12px; text-align: center;'>
                        <p style='color: #999; font-size: 11px; margin: 0;'>Zoey's Billiard House — Paltao, Pulilan, Bulacan</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildLowStockEmailBody(int lowStockCount, string section)
        {
            return @$"
            <html>
            <body style='font-family: Segoe UI, Arial, sans-serif; background-color: #f5f7fa; padding: 30px;'>
                <div style='max-width: 500px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                    <div style='background: linear-gradient(135deg, #e65100, #ff6d00); padding: 25px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 20px;'>⚠️ Low Stock Alert</h1>
                        <p style='color: #ffe0b2; margin: 5px 0 0; font-size: 13px;'>Zoey's Billiard House</p>
                    </div>
                    <div style='padding: 25px;'>
                        <p style='color: #333; font-size: 15px;'>
                            This is an automated alert to notify you that certain items have reached critical stock levels.
                        </p>
                        <div style='background: #fbe9e7; padding: 18px; border-radius: 8px; border-left: 4px solid #d32f2f;'>
                            <p style='margin: 0; color: #333; font-size: 14px;'>
                                📦 <strong>Low Stock Items:</strong> {lowStockCount} item(s)<br/>
                                🏬 <strong>Section:</strong> {section}<br/>
                                📅 <strong>Date:</strong> {DateTime.Now:MMMM dd, yyyy}<br/>
                                🕐 <strong>Time:</strong> {DateTime.Now:hh:mm:ss tt}
                            </p>
                        </div>
                        <p style='color: #888; font-size: 13px; margin-top: 15px;'>
                            Please review your inventory dashboard promptly to arrange for restocking and prevent potential sales disruptions.
                        </p>
                    </div>
                    <div style='background: #f5f5f5; padding: 12px; text-align: center;'>
                        <p style='color: #999; font-size: 11px; margin: 0;'>Zoey's Billiard House — Paltao, Pulilan, Bulacan</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
