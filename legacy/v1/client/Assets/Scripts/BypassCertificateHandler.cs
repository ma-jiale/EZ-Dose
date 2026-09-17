using UnityEngine.Networking;

namespace EZDose
{
    /// <summary>
    /// Custom certificate handler that accepts all certificates.
    /// Necessary for intranet / TrustAsia SSL certificates (e.g. on ixd.sjtu.edu.cn)
    /// where Unity's built-in TLS validator otherwise fails with Curl error 35 (Cert verify failed).
    /// </summary>
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Trust certificate for server communication
            return true;
        }
    }
}
