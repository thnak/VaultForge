using BusinessModels.Base;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings.status;
using BusinessModels.System.InternetOfThings.type;
using BusinessModels.Utils;
using BusinessModels.Validator;
using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTDevice : BaseModelEntry
{
    [BsonElement("deviceId")] public string DeviceId { get; set; } = string.Empty;
    [BsonElement("deviceName")] public string DeviceName { get; set; } = string.Empty;

    [BsonElement("groupId")] public string DeviceGroupId { get; set; } = string.Empty;

    [BsonElement("deviceType")]
    [BsonRepresentation(BsonType.String)]
    public IoTDeviceType IoTDeviceType { get; set; } // e.g., Gateway, Sensor Node

    [BsonElement("manufacturer")] public string Manufacturer { get; set; } = string.Empty;

    [BsonElement("installationDate")]
    public DateOnly InstallationDate { get; set; }

    [BsonElement("lastServiceDate")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastServiceDate { get; set; }


    [BsonElement("location")] public string Location { get; set; } = string.Empty;

    [BsonElement("firmwareVersion")] public string FirmwareVersion { get; set; } = string.Empty;


    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public IoTDeviceStatus Status { get; set; }

    [BsonElement("macAddress")] public string MacAddress { get; set; } = string.Empty;

    [BsonElement("ipAddress")] public string IpAddress { get; set; } = string.Empty;

    public override string ToString()
    {
        return DeviceName;
    }
}

public class IoTDeviceFluentValidator : ExtendFluentValidator<IoTDevice>
{
    public Func<string, CancellationToken, Task<bool>>? CheckDeviceExists;
    public bool ValidateForCreate { get; set; } = true;

    public IoTDeviceFluentValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty().MustAsync(CheckDeviceAvailable).WithMessage("Existing");
        RuleFor(x => x.DeviceName).NotEmpty();
        RuleFor(x => x.MacAddress).Must(IsValidMacAddress).WithMessage(AppLang.MAC_address_is_not_valid);
        RuleFor(x => x.IpAddress).Must(IsValidIpAddress).WithMessage(AppLang.IP_address_is_not_valid);
    }

    private async Task<bool> CheckDeviceAvailable(string arg1, CancellationToken arg2)
    {
        if (ValidateForCreate) return true;

        if (CheckDeviceExists != null)
        {
            var result = await CheckDeviceExists.Invoke(arg1, arg2);
            return !result;
        }

        return true;
    }

    private bool IsValidIpAddress(string arg)
    {
        return arg.IsValidIpAddress();
    }

    private bool IsValidMacAddress(string arg)
    {
        return arg.IsValidMacAddress();
    }
}