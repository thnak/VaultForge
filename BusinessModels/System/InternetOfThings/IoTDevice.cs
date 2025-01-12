using BusinessModels.Attribute;
using BusinessModels.Base;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings.status;
using BusinessModels.System.InternetOfThings.type;
using BusinessModels.Utils;
using BusinessModels.Validator;
using FluentValidation;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTDevice : BaseModelEntry
{
    [IndexedDbKey]
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceGroupId { get; set; } = string.Empty;
    public IoTDeviceType IoTDeviceType { get; set; } // e.g., Gateway, Sensor Node
    public string Manufacturer { get; set; } = string.Empty;
    public DateOnly InstallationDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastServiceDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public IoTDeviceStatus Status { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public override string ToString()
    {
        return DeviceName;
    }
}

public class IoTDeviceFluentValidator : ExtendFluentValidator<IoTDevice>
{
    public Func<string, CancellationToken, Task<bool>>? CheckDeviceExists;

    /// <summary>
    /// return true when ip address has been used
    /// </summary>
    public Func<string, CancellationToken, Task<Result<bool>>>? CheckUsedIpAddress;

    /// <summary>
    /// return true when mac address has been used
    /// </summary>
    public Func<string, CancellationToken, Task<Result<bool>>>? CheckUsedMacAddress;


    public bool ValidateForCreate { get; set; } = true;

    public IoTDeviceFluentValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty().MustAsync(CheckDeviceAvailable).WithMessage(AppLang.Existing);
        RuleFor(x => x.DeviceName).NotEmpty();
        RuleFor(x => x.MacAddress).Must(IsValidMacAddress).WithMessage(AppLang.MAC_address_is_not_valid).MustAsync(CheckAvailableAddress).WithMessage("{error}");
        RuleFor(x => x.IpAddress).Must(IsValidIpAddress).WithMessage(AppLang.IP_address_is_not_valid).MustAsync(CheckAvailableIpAddress).WithMessage("{error}");
    }

    private async Task<bool> CheckAvailableIpAddress(IoTDevice arg1, string arg2, ValidationContext<IoTDevice> arg3, CancellationToken arg4)
    {
        if (ValidateForCreate) return true;
        if (CheckUsedMacAddress != null)
        {
            var result = await CheckUsedMacAddress.Invoke(arg2, arg4);
            if (!result.IsSuccess)
            {
                arg3.MessageFormatter.AppendArgument("error", result.Message);
                return false;
            }

            if (result.Value)
            {
                arg3.MessageFormatter.AppendArgument("error", result.Message);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CheckAvailableAddress(IoTDevice arg1, string arg2, ValidationContext<IoTDevice> arg3, CancellationToken arg4)
    {
        if (ValidateForCreate) return true;
        if (CheckUsedIpAddress != null)
        {
            var result = await CheckUsedIpAddress.Invoke(arg2, arg4);
            if (!result.IsSuccess)
            {
                arg3.MessageFormatter.AppendArgument("error", result.Message);
                return false;
            }

            if (result.Value)
            {
                arg3.MessageFormatter.AppendArgument("error", result.Message);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CheckDeviceAvailable(string arg1, CancellationToken arg2)
    {
        if (ValidateForCreate) return true;

        if (CheckDeviceExists != null)
        {
            var result = await CheckDeviceExists.Invoke(arg1, arg2);
            return result;
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