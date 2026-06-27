using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using gym_management_server.Fingerprints;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class AesTemplateProtectorTests
    {
        private static AesTemplateProtector CreateProtector()
        {
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();
            return new AesTemplateProtector(config);
        }

        [Fact]
        public void Protect_then_Unprotect_round_trips()
        {
            var protector = CreateProtector();
            var plaintext = new byte[] { 10, 20, 30, 40, 50, 60 };

            var protectedBytes = protector.Protect(plaintext);
            var recovered = protector.Unprotect(protectedBytes);

            Assert.Equal(plaintext, recovered);
        }

        [Fact]
        public void Protected_output_is_not_plaintext()
        {
            var protector = CreateProtector();
            var plaintext = new byte[] { 1, 2, 3, 4, 5 };

            var protectedBytes = protector.Protect(plaintext);

            Assert.NotEqual(plaintext, protectedBytes);
            Assert.True(protectedBytes.Length > plaintext.Length); // nonce + tag overhead
        }

        [Fact]
        public void Encryption_uses_fresh_nonce_each_call()
        {
            var protector = CreateProtector();
            var plaintext = new byte[] { 7, 7, 7, 7 };

            var first = protector.Protect(plaintext);
            var second = protector.Protect(plaintext);

            Assert.NotEqual(first, second); // different nonce -> different ciphertext
        }

        [Fact]
        public void Tampered_ciphertext_fails_authentication()
        {
            var protector = CreateProtector();
            var protectedBytes = protector.Protect(new byte[] { 1, 2, 3, 4 });
            protectedBytes[^1] ^= 0xFF; // flip a bit in the ciphertext

            Assert.Throws<AuthenticationTagMismatchException>(() => protector.Unprotect(protectedBytes));
        }

        [Fact]
        public void Missing_key_throws()
        {
            var config = new ConfigurationBuilder().Build();
            Assert.Throws<InvalidOperationException>(() => new AesTemplateProtector(config));
        }

        [Fact]
        public void Wrong_key_length_throws()
        {
            var shortKey = Convert.ToBase64String(new byte[16]);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = shortKey })
                .Build();
            Assert.Throws<InvalidOperationException>(() => new AesTemplateProtector(config));
        }
    }
}
