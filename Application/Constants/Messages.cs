namespace Application.Constants
{
    public static class Messages
    {
        public static class Connection
        {
            public const string Established = "Connection established.";
            public const string Rejected = "Connection rejected.";
            public const string NotAuthorized = "Connection not authorized.";
            public const string UploadingChunk = "Uploading chunk...";
            public const string UploadSuccessful = "Upload completed successfully.";
            public const string InvalidConnectionId = "Connection ID cannot be null or empty.";
        }

        public static class Certificate
        {
            public const string CreateCertificateSuccess = "Create certificate successful.";
            public const string InvalidFilePath = "Invalid file path.";
            public const string InvalidPassword = "Invalid password.";
        }

        public static class Encryption
        {
            public const string InvalidKeyLength = "Invalid key length. Expected 32 bytes for AES-256.";
            public const string InvalidBase64Key = "Invalid Base64 encryption key format.";
            public const string InvalidConnectionId = "Invalid connection ID for encryption operation.";
            public const string EncryptedDataTooShort = "Encrypted data is too short.";
            public const string FailedToDecrypt = "Decryption failed.";
            public const string FailedToGenKey = "Failed to generate encryption key.";
            public const string FailedToRemoveKey = "Failed to remove encryption key.";
            public const string KeyNotFound = "Encryption key not found for connection.";
        }

        public static class Session
        {
            public const string FailedToCreateSession = "Failed to create session.";
            public const string DisposingError = "Error disposing file stream.";
            public const string InactiveSession = "No active upload session found.";
            public const string InvalidSessionState = "Invalid session state.";
        }

        public static class FileValidation
        {
            public const string InvalidFileName = "Invalid file name.";
            public const string InvalidFileSize = "Invalid file size.";
            public const string InvalidChecksum = "Invalid checksum.";
            public const string InvalidChunkNumber = "Invalid chunk number.";
            public const string InvalidChunkSize = "Invalid chunk size.";
            public const string InvalidFileStream = "File stream is not available.";
            public const string InvalidFilePath = "Invalid file path detected.";
            public const string IncompleteUpload = "Upload incomplete. Missing chunks.";
            public const string FileNotFound = "File not found.";
            public const string FileSizeMissMatch = "File size mismatch. Expected: {0}, Actual: {1}.";
            public const string FailedToDelete = "File not found.";
            public const string FailedToUpload = "Failed to upload.";
            public const string FailedToCreateDirectory = "Failed to create upload directory.";
            public const string UploadInProgress = "An upload is already in progress.";
            public const string UploadSuccessful = "Upload completed successfully.";
            public const string UnsupportedFileType = "Unsupported file type.";
            public const string CannotUploadInState = "Cannot upload file in state: {0}.";
        }
        public static class ShellExecution
        {
            public const string ProcessStartFailed = "Failed to start shell process.";
            public const string EmptyOutput = "Shell command returned empty output.";
            public const string SystemInfoRetrievalFailed = "Failed to get system information.";
        }
    }
}
