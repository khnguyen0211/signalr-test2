namespace Application.Enums;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstallationStatusEnum
{
    Installed,
    NotInstalled,
    Checking,
}
