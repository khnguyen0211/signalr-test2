using System.Collections.Concurrent;
using System.Security.Cryptography;
using Application.Constants;
using Application.Services.Interfaces;

namespace Application.Services.Implementations
{
    public class EncryptionService : IEncryptionService
    {
        private static readonly Lazy<EncryptionService> _instance = new(() => new EncryptionService());
        public static EncryptionService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, byte[]> _encryptionKeys = new();

        private EncryptionService() { }

        public byte[] DecryptChunk(string connectionId, byte[] encryptedData)
        {
            if (!_encryptionKeys.TryGetValue(connectionId, out var encryptionKey))
                throw new InvalidOperationException(Messages.Encryption.KeyNotFound);

            try
            {
                return DecryptWithAesGcm(encryptedData, encryptionKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public bool RemoveEncryptionKey(string connectionId)
        {
            return _encryptionKeys.TryRemove(connectionId, out _);
        }
        private byte[] DecryptWithAesGcm(byte[] encryptedData, byte[] key)
        {
            const int ivLength = 12;
            const int tagLength = 16;

            if (encryptedData.Length < ivLength + tagLength)
                throw new ArgumentException(Messages.Encryption.EncryptedDataTooShort);

            var iv = encryptedData.AsSpan(0, ivLength);
            var ciphertextWithTag = encryptedData.AsSpan(ivLength);
            var ciphertext = ciphertextWithTag[..^tagLength];
            var tag = ciphertextWithTag[^tagLength..];

            using var aes = new AesGcm(key, tagSizeInBytes: 16);
            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(iv, ciphertext, tag, plaintext);

            return plaintext;
        }

        public string GenerateEncryptionKey(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException(Messages.Connection.InvalidConnectionId, nameof(connectionId));
            try
            {
                var keyBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(keyBytes);

                _encryptionKeys[connectionId] = keyBytes;
                return Convert.ToBase64String(keyBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{Messages.Encryption.FailedToGenKey} {ex.Message}", ex);
            }
        }
    }
}
