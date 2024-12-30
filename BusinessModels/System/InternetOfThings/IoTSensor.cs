using BusinessModels.Base;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings.status;
using BusinessModels.System.InternetOfThings.type;
using BusinessModels.Validator;
using FluentValidation;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.InternetOfThings;

public class IoTSensor : BaseModelEntry
{
    public string SensorId { get; set; } = string.Empty;


    public string SensorName { get; set; } = string.Empty;


    public IoTSensorType IoTSensorType { get; set; } // Enum for sensor types

    public string DeviceId { get; set; } = string.Empty; // Foreign key to Device


    public string UnitOfMeasurement { get; set; } = string.Empty; // e.g., °C, %, Pa

    public float Accuracy { get; set; }


    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CalibrationTime { get; set; }

    public float Rotate { get; set; }

    public bool FlipHorizontal { get; set; }
    public bool FlipVertical { get; set; }


    public IoTSensorStatus Status { get; set; }

    public override string ToString()
    {
        return SensorName;
    }
}

public class IoTSensorFluentValidator : ExtendFluentValidator<IoTSensor>
{
    public Func<string, CancellationToken, Task<bool>>? CheckDeviceExists;
    public Func<string, CancellationToken, Task<bool>>? CheckSensorExists;
    public bool ValidateForCreate { get; set; } = true;

    public IoTSensorFluentValidator()
    {
        RuleFor(x => x.SensorId).NotEmpty().MustAsync(CheckSensorAvailable).WithMessage(AppLang.Already_existing);
        RuleFor(x => x.SensorName).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty().MustAsync(CheckDeviceAvailable).WithMessage(AppLang.Device_not_found);
    }

    private async Task<bool> CheckSensorAvailable(string arg1, CancellationToken arg2)
    {
        if (ValidateForCreate) return true;
        if (CheckSensorExists != null)
        {
            var result = await CheckSensorExists.Invoke(arg1, arg2);
            return result;
        }

        return true;
    }

    private async Task<bool> CheckDeviceAvailable(string arg1, CancellationToken arg2)
    {
        if (CheckDeviceExists != null)
        {
            var result = await CheckDeviceExists.Invoke(arg1, arg2);
            return result;
        }

        return true;
    }
}