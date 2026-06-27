using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace gym_management_server.Fingerprints
{
    /// <summary>
    /// AES-256-GCM protector for fingerprint templates. The 256-bit key is supplied as a base64 string
    /// via configuration ("Fingerprint:EncryptionKey", ideally from user-secrets / environment).
    /// Output layout: [12-byte nonce][16-byte tag][ciphertext].
    /// </summary>
    public class AesTemplateProtector : ITemplateProtector
    {
        private const int NonceSize = 12; // 96-bit nonce recommended for GCM
        private const int TagSize = 16;   // 128-bit auth tag
        private readonly byte[] _key;

        public AesTemplateProtector(IConfiguration configuration)
        {
            var base64Key = configuration["Fingerprint:EncryptionKey"];
            if (string.IsNullOrWhiteSpace(base64Key))
                throw new InvalidOperationException(
                    "Fingerprint:EncryptionKey is not configured. Provide a base64-encoded 256-bit AES key.");

            _key = Convert.FromBase64String(base64Key);
            if (_key.Length != 32)
                throw new InvalidOperationException("Fingerprint:EncryptionKey must decode to 32 bytes (256-bit).");
        }

        public byte[] Protect(byte[] plaintextTemplate)
        {
            if (plaintextTemplate == null) throw new ArgumentNullException(nameof(plaintextTemplate));

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plaintextTemplate.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plaintextTemplate, ciphertext, tag);

            var output = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, output, NonceSize + TagSize, ciphertext.Length);
            return output;
        }

        public byte[] Unprotect(byte[] protectedTemplate)
        {
            if (protectedTemplate == null) throw new ArgumentNullException(nameof(protectedTemplate));
            if (protectedTemplate.Length < NonceSize + TagSize)
                throw new ArgumentException("Protected template is malformed (too short).", nameof(protectedTemplate));

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ciphertextLength = protectedTemplate.Length - NonceSize - TagSize;
            var ciphertext = new byte[ciphertextLength];

            Buffer.BlockCopy(protectedTemplate, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(protectedTemplate, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(protectedTemplate, NonceSize + TagSize, ciphertext, 0, ciphertextLength);

            var plaintext = new byte[ciphertextLength];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
    }
}
