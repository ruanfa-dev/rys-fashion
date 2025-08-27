using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using Serilog;

namespace Web.Api.Infrastructure.Configuration;

public static class JsonOptionsConfiguration
{
    public static IServiceCollection AddJsonConfig(this IServiceCollection services)
    {

        // Configure System.Text.Json for controllers
        services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new SnakeCaseQueryModelBinderProvider());
        })
            .AddJsonOptions(options => ConfigureJsonOptions(options.JsonSerializerOptions));

        // Configure System.Text.Json for HTTP (used by TypedResults) Minimal API
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
            options => ConfigureJsonOptions(options.SerializerOptions));

        Log.Information("Register: JSON Configuration");
        return services;
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions options)
    {
        options.IncludeFields = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.Converters.Add(new SystemTextJsonUtcDateTimeOffsetConverter());
        options.Converters.Add(new SystemTextJsonNullableDateTimeOffsetConverter());
    }

    /// <summary>
    /// Converter for DateTimeOffset that serializes to UTC with "Z" suffix.
    /// </summary>
    public class SystemTextJsonUtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString))
                    throw new JsonException("Cannot convert empty string to DateTimeOffset.");

                if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                    return dto;

                throw new JsonException($"Invalid date format: {dateString}");
            }

            throw new JsonException($"Unexpected token parsing date. Expected String, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            string isoString = value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "Z";
            writer.WriteStringValue(isoString);
        }
    }

    /// <summary>
    /// Converter for nullable DateTimeOffset that serializes to UTC with "Z" suffix or null.
    /// </summary>
    public class SystemTextJsonNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString))
                    return null;

                if (DateTimeOffset.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                    return dto;

                throw new JsonException($"Invalid date format: {dateString}");
            }

            throw new JsonException($"Unexpected token parsing date. Expected String or Null, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                string isoString = value.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "Z";
                writer.WriteStringValue(isoString);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    public class SnakeCaseQueryModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.IsComplexType)
            {
                return new SnakeCaseComplexTypeModelBinder();
            }
            return null;
        }
    }

    public class SnakeCaseComplexTypeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelMetadata.ModelType;
            var model = Activator.CreateInstance(modelType);

            if (model == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            foreach (var property in modelType.GetProperties())
            {
                var snakeCaseName = ConvertToSnakeCase(property.Name);
                var value = bindingContext.ValueProvider.GetValue(snakeCaseName);

                if (value != ValueProviderResult.None && !string.IsNullOrEmpty(value.FirstValue))
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value.FirstValue, property.PropertyType);
                        property.SetValue(model, convertedValue);
                    }
                    catch
                    {
                        // Handle conversion errors gracefully
                        continue;
                    }
                }
            }

            bindingContext.Result = ModelBindingResult.Success(model);
            return Task.CompletedTask;
        }

        private string ConvertToSnakeCase(string input)
        {
            return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
        }
    }
}