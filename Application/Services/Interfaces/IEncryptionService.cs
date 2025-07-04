namespace Application.Services.Interfaces
{
    public interface IEncryptionService
    {
        string GenerateEncryptionKey(string connectionId);
        byte[] DecryptChunk(string connectionId, byte[] encryptedData);
        bool RemoveEncryptionKey(string connectionId);
    }
}
