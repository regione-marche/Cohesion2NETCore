using Microsoft.Extensions.Options;
using System.Text;
using System.Web;
using System.Xml;
using Flurl.Http;
using CohesionNETCore.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;


namespace CohesionNETCore.Services
{
    public interface CohesionService
    {
        public interface ICohesionService
        {
            string RequestAuth(string urlValidation, string urlReturn);
            AuthCohesionCheckResponse CheckAuth(string auth);
            void LogoutFE(ISession _session);
        }
        public class CohesionService : ICohesionService
        {
            private IOptions<AppSettings> _appSettings;

            public CohesionService(IOptions<AppSettings> appSettings)
            {
                _appSettings = appSettings;
            }

            public string RequestAuth(string urlValidation, string urlReturn)
            {
                string idSito = _appSettings.Value.SiteIdSito;
                string ssoAdditional = _appSettings.Value.SSOadditionalData;
                string ssoCheckUrl = _appSettings.Value.SSOcheckUrl;

                urlReturn =
                    urlReturn == null
                    ? ""
                    : urlReturn;

                string auth =
                    "<dsAuth xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                    "xmlns=\"http://tempuri.org/Auth.xsd\">" +
                        "<auth>" +
                            $"<user/><id_sa /><id_sito>{idSito}</id_sito>" +
                            "<esito_auth_sa /><id_sessione_sa /><id_sessione_aspnet_sa />" +
                            $"<url_validate>{urlValidation.Replace("&", "&amp;")}</url_validate>" +
                            $"<url_richiesta>{urlReturn.Replace("&", "&amp;")}</url_richiesta>" +
                            "<esito_auth_sso /><id_sessione_sso /><id_sessione_aspnet_sso />" +
                            $"<stilesheet>{ssoAdditional}</stilesheet>" +
                        "</auth>" +
                    "</dsAuth>";
                auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(auth));
                return $"{ssoCheckUrl}?auth={HttpUtility.UrlEncode(auth)}";
            }

            public AuthCohesionCheckResponse CheckAuth(string auth)
            {
                var res = new AuthCohesionCheckResponse();
                var authDecoded = Encoding.ASCII.GetString(Convert.FromBase64String(auth));
                var authXml = new XmlDocument();
                var ssoWebCheckSession = _appSettings.Value.SSOwebCheckSession;

                authXml.LoadXml(authDecoded);
                string esitoAuthSSO = authXml.GetElementsByTagName("esito_auth_sso")[0].InnerText;
                if (esitoAuthSSO.Equals("OK"))
                {
                    string idsessioneSSO =
                        authXml.GetElementsByTagName("id_sessione_sso").Count > 0
                        ? authXml.GetElementsByTagName("id_sessione_sso")[0].InnerText
                        : null;
                    string idsessioneSSOASPNET =
                        authXml.GetElementsByTagName("id_sessione_aspnet_sso").Count > 0
                        ? authXml.GetElementsByTagName("id_sessione_aspnet_sso")[0].InnerText
                        : null;
                    bool loginPerCittadini =
                        authXml.GetElementsByTagName("cittad").Count > 0 &&
                        authXml.GetElementsByTagName("cittad")[0].InnerText == "1";
                    string tipoIDP =
                        authXml.GetElementsByTagName("tipo_idp").Count > 0
                        ? authXml.GetElementsByTagName("tipo_idp")[0].InnerText
                        : "Cohesion";

                    string token = webCheckSessionSSO(
                        ssoWebCheckSession,
                        "GetCredential",
                        idsessioneSSO,
                        idsessioneSSOASPNET);

                    if (token != null && !token.Equals("") && token.IndexOf("<AUTH>NO</AUTH>") == -1)
                    {
                        // autenticato
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(token);

                        // creo oggetto utente da ritornare
                        //var user = new Utente();
                        //user.CodiceFiscale = doc.GetElementsByTagName("codice_fiscale")[0].InnerText;
                        res.user = doc.GetElementsByTagName("codice_fiscale")[0].InnerText;
                        res.idsessioneSSO = idsessioneSSO;
                        res.idsessioneSSOASPNET = idsessioneSSOASPNET;
                    }
                    else
                    {
                        // non autenticato
                        res.error = true;
                        res.errorDescription = $"Errore durante l'autenticazione.\nInfo token:{token}";
                    }
                }
                else
                {
                    res.error = true;
                    res.errorDescription = "Errore durante l'autenticazione";
                }
                return res;
            }

            public void LogoutFE(ISession _session)
            {
                // prendo l'url per effettuare la chiamata di logout a cohesion
                var ssoWebCheckSession = _appSettings.Value.SSOwebCheckSession;

                // prendo le variabili dalla sessione
                string idsessioneSSO = _session.GetString("idsessioneSSO");
                string idsessioneSSOASPNET = _session.GetString("idsessioneSSOASPNET");

                // effettuo la chiamata
                webCheckSessionSSO(
                    ssoWebCheckSession,
                    "LogoutSito",
                    idsessioneSSO,
                    idsessioneSSOASPNET);
            }

            private string webCheckSessionSSO(string url, string operation, string idsessioneSSO, string idsessioneSSOASPNET)
            {
                var param = $"Operation={operation}&IdSessioneSSO={idsessioneSSO}&IdSessioneASPNET={idsessioneSSOASPNET}";

                // new
                var result = Task.Run(
                    async () =>
                        await (url + "?" + param)
                        .WithHeader("Accept", "application/json")
                        .GetAsync()
                        .ReceiveString()
                        .ConfigureAwait(false)
                    )
                    .GetAwaiter()
                    .GetResult();
                return result;
            }
        }
    }
}
