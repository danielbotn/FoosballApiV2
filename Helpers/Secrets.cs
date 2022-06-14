namespace FoosballApi.Helpers
{
    public class Secrets
    {
        public string JWTSecret { get; set; }

        public string SmtpEmailFrom { get; set; }

        public string SmtpHost { get; set; }

        public string SmtpPort { get; set; }

        public string SmtpUser { get; set; }

        public string SmtpPass { get; set; }

        public string DatoCmsBearer { get; set; }
    }
}