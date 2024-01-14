namespace CohesionNETCore.Models
{
    public class AuthCohesionCheckResponse
    {
        public string redirectUrl { get; set; }
        public string errorDescription { get; set; }
        public string user { get; set; }
        public string idsessioneSSO { get; set; }
        public string idsessioneSSOASPNET { get; set; }
        public bool error { get; set; } = false;
    }
}
