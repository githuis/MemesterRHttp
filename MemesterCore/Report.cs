namespace MemesterCore
{
    class Report
    {
        public Report()
        {
            
        }

        public Report(string m, ReportReason rr, string email, string reason)
        {
            Meme = m;
            ReasonNo = rr;
            Email = email;
            Reason = reason;
        }

        public string Meme { get; set; }
        public ReportReason ReasonNo { get; set; }
        public string Reason { get; set; }
        public string Email { get; set; }

        public enum ReportReason
        {
            NSFW = 0,
            Abuse = 1,
            CopyrightClaim = 2,
            TooKorean = 3,
            Other = 4
        }
    }
}