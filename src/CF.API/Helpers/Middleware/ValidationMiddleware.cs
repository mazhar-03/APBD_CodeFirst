using System.Text.Json;
using System.Text.RegularExpressions;
using CF.API.DAL;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Helpers.Middleware;

public class ValidationMiddleware
{
    private readonly ILogger<ValidationMiddleware> _logger;
    private readonly RequestDelegate _next;

    //life-saver for the problem that i faced.(can't use dbcontext here)
    private readonly IServiceProvider _serviceProvider;
    private readonly string _validationRulesFilePath = "example_validation_rules.json";

    public ValidationMiddleware(
        RequestDelegate next,
        ILogger<ValidationMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/devices") &&
            (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put))
        {
            var deviceJson = await GetDeviceJsonFromBody(context);

            if (deviceJson == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid device data.");
                return;
            }

            var root = deviceJson.RootElement;
            if (!root.TryGetProperty("typeId", out var typeIdProp) ||
                !root.TryGetProperty("isEnabled", out var isEnabledProp) ||
                !root.TryGetProperty("additionalProperties", out var additionalProps))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing required fields (typeId, isEnabled, additionalProperties).");
                return;
            }

            var typeId = typeIdProp.GetInt32();
            var isEnabled = isEnabledProp.GetBoolean();
            string? deviceTypeName = null;

            // solved the issue that we cant use dbcontext here in that way
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var type = await db.DeviceTypes.FirstOrDefaultAsync(t => t.Id == typeId);
                deviceTypeName = type?.Name;
            }

            // skip the validation for not maching any deviceType
            if (deviceTypeName == null)
            {
                _logger.LogWarning($"Device type not found: {typeId}");
                await _next(context);
                return;
            }

            var rulesJson = await File.ReadAllTextAsync(_validationRulesFilePath);
            var rulesDoc = JsonDocument.Parse(rulesJson);
            var validations = rulesDoc.RootElement.GetProperty("validations").EnumerateArray();

            var matchedRule = validations.FirstOrDefault(v =>
                v.GetProperty("type").GetString() == deviceTypeName &&
                v.GetProperty("preRequestName").GetString() == "isEnabled" &&
                v.GetProperty("preRequestValue").GetString().ToLower() == isEnabled.ToString().ToLower()
            );

            // if there are rules, we apply
            if (matchedRule.ValueKind != JsonValueKind.Undefined)
            {
                foreach (var rule in matchedRule.GetProperty("rules").EnumerateArray())
                {
                    var paramName = rule.GetProperty("paramName").GetString();

                    if (!additionalProps.TryGetProperty(paramName, out var paramValue))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync($"Missing required additional property: {paramName}");
                        _logger.LogWarning($"Missing property: {paramName}");
                        return;
                    }

                    if (rule.TryGetProperty("regex", out var regexProp))
                    {
                        if (regexProp.ValueKind == JsonValueKind.Array)
                        {
                            var allowed = regexProp.EnumerateArray().Select(x => x.GetString()).ToList();
                            if (!allowed.Contains(paramValue.GetString()))
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsync(
                                    $"Invalid value for {paramName}. Allowed: {string.Join(", ", allowed)}");
                                _logger.LogWarning($"Invalid value for {paramName}: {paramValue}");
                                return;
                            }
                        }
                        else if (regexProp.ValueKind == JsonValueKind.String)
                        {
                            var regex = regexProp.GetString();
                            if (!Regex.IsMatch(paramValue.GetString() ?? "", regex))
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsync($"Invalid value for {paramName} (regex: {regex})");
                                _logger.LogWarning($"Regex failed for {paramName}: {regex}");
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                await _next(context);
                return;
            }
        }

        await _next(context);
    }

    private async Task<JsonDocument> GetDeviceJsonFromBody(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            var doc = await JsonDocument.ParseAsync(context.Request.Body);
            context.Request.Body.Position = 0;
            return doc;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error parsing device JSON: " + ex.Message);
            return null;
        }
    }
}