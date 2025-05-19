using System.Reflection;

namespace LawyerApp.Services
{
    public class PublicKeyProvider
    {
        private const string ResourceName =
            "LawyerApp.Certificates.LawyerPubKey.pem";

        public static string GetPublicKeyPem()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(ResourceName)
                         ?? throw new FileNotFoundException(ResourceName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
