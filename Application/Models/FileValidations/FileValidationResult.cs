namespace Application.Models.FileValidations
{
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
