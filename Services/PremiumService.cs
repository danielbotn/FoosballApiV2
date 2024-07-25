using FoosballApi.Models;

namespace FoosballApi.Services
{
    public interface IPremiumService
    {
        string GenerateFoosballCertificate(User user, string leagueName);
    }

    public class PremiumService : IPremiumService
    {
        public string GenerateFoosballCertificate(User user, string leagueName)
        {
            string fullName = $"{user.FirstName} {user.LastName}";
            string currentDate = DateTime.Now.ToString("MMMM d, yyyy");

            string certificateHtml = $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Foosball League Champion Certificate</title>
                <style>
                    body {{
                        font-family: 'Times New Roman', serif;
                        background-color: #f0f0f0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                    }}
                    .certificate {{
                        background-color: #fff;
                        border: 2px solid #golden;
                        padding: 40px;
                        width: 800px;
                        text-align: center;
                        box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
                        background-image: url('data:image/svg+xml;utf8,<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"" viewBox=""0 0 100 100""><rect width=""100"" height=""100"" fill=""none"" stroke=""%23golden"" stroke-width=""2""/></svg>');
                        background-repeat: repeat;
                        background-size: 20px 20px;
                        position: relative;
                    }}
                    .content {{
                        background-color: rgba(255, 255, 255, 0.9);
                        padding: 20px;
                        border: 1px solid #golden;
                    }}
                    h1 {{
                        font-size: 36px;
                        color: #333;
                        margin-bottom: 20px;
                        font-weight: normal;
                    }}
                    .recipient {{
                        font-size: 28px;
                        font-weight: bold;
                        color: #1a5f7a;
                        margin: 30px 0;
                    }}
                    .description {{
                        font-size: 18px;
                        color: #666;
                        margin-bottom: 30px;
                    }}
                    .icon {{
                        width: 100px;
                        height: 100px;
                        margin: 20px auto;
                    }}
                    .signature {{
                        font-style: italic;
                        margin-top: 40px;
                    }}
                    .seal {{
                        position: absolute;
                        bottom: 20px;
                        right: 20px;
                        width: 80px;
                        height: 80px;
                        border: 2px solid #golden;
                        border-radius: 50%;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        font-weight: bold;
                        color: #golden;
                        font-size: 14px;
                    }}
                </style>
            </head>
            <body>
                <div class=""certificate"">
                    <div class=""content"">
                        <h1>Certificate of Achievement</h1>
                        <div class=""icon"">
                            <!-- SVG code here -->
                        </div>
                        <p>This is to certify that</p>
                        <div class=""recipient"">{fullName}</div>
                        <p class=""description"">Has emerged victorious in the {leagueName} Championship</p>
                        <p>Awarded on <span id=""current-date"">{currentDate}</span></p>
                        <div class=""signature"">League Director</div>
                    </div>
                    <div class=""seal"">OFFICIAL SEAL</div>
                </div>
            </body>
            </html>";

            return certificateHtml;
        }
    }
}
