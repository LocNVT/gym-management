namespace gym_management_server.Fingerprints
{
    /// <summary>
    /// Encrypts/decrypts biometric template bytes at rest. Templates are only ever decrypted
    /// transiently in memory during matching; they are never persisted or logged in plaintext.
    /// </summary>
    public interface ITemplateProtector
    {
        byte[] Protect(byte[] plaintextTemplate);
        byte[] Unprotect(byte[] protectedTemplate);
    }
}
