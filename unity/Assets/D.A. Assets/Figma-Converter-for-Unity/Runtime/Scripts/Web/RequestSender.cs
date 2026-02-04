using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.Logging;
using DA_Assets.Networking;
using System;
using System.Net;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using DA_Assets.Shared;

#pragma warning disable IDE0052

namespace DA_Assets.FCU
{
    [Serializable]
    public class RequestSender : FcuBase
    {
        [SerializeField] float pbarProgress;
        public float PbarProgress => pbarProgress;

        [SerializeField] float pbarBytes;
        public float PbarBytes => pbarBytes;

        private const HttpStatusCode RateLimitStatusCode = HttpStatusCode.TooManyRequests;
        private static readonly TimeSpan RateLimitWindowThreshold = TimeSpan.FromMinutes(10);

        public RequestHeader GetRequestHeader(string token, AuthType? authType = null)
        {
            AuthType currentAuthType = authType != null ? (AuthType)authType : monoBeh.Authorizer.CurrentSession.AuthType;

            switch (currentAuthType)
            {
                case AuthType.OAuth2:
                    return new RequestHeader
                    {
                        Name = "Authorization",
                        Value = $"Bearer {token}"
                    };
                case AuthType.Manual:
                    return new RequestHeader
                    {
                        Name = "X-Figma-Token",
                        Value = $"{token}"
                    };
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<DAResult<T>> SendRequest<T>(DARequest request, CancellationToken token, string customErrorText = null)
        {
            while (true)
            {
                UnityHttpClient webRequest;

                switch (request.RequestType)
                {
                    case RequestType.Post:
                        webRequest = UnityHttpClient.Post(request.Query, request.WWWForm);
                        break;
                    default:
                        webRequest = UnityHttpClient.Get(request.Query);
                        break;
                }

                if (monoBeh.IsDebug())
                {
                    Debug.Log(request.Query);
                }

                using (webRequest)
                {
                    DAResult<T> result;
                    bool isNetworkException = false;
                    string currentErrorText = customErrorText;


                    if (request.RequestHeader.IsDefault() == false)
                    {
                        webRequest.SetRequestHeader(request.RequestHeader.Name, request.RequestHeader.Value);
                    }

                    try
                    {
                        _ = webRequest.SendWebRequest(token);

                        await UpdateRequestProgressBar(webRequest);
                        await MoveRequestProgressBarToEnd();

                        result = new DAResult<T>();

                        if (request.RequestType == RequestType.GetFile)
                        {
                            result.Success = true;
                            result.Object = (T)(object)webRequest.downloadHandler.data;
                        }
                        else
                        {
                            _ = request.WriteLog(webRequest);

                            string text = webRequest.downloadHandler.text;

                            if (typeof(T) == typeof(string))
                            {
                                result.Success = true;
                                result.Object = (T)(object)text;
                            }
                            else
                            {
                                result = await TryParseResponse<T>(text, request, webRequest);
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        result = CreateErrorResult<T>(
                            (int)WR_Result.ConnectionError,
                            FcuLocKey.log_enable_http_project_settings.Localize());

                        monoBeh.AssetTools.StopAsset(StopAssetReason.Error);
                        isNetworkException = true;
                    }
                    catch (Exception ex)
                    {
                        result = CreateErrorResult<T>(
                            (int)WR_Result.ProtocolError,
                            ex.Message,
                            ex);

                        isNetworkException = true;
                    }

                    if (result.Success == false)
                    {
                        LogRequestError(request, result.Error, currentErrorText);
                    }

                    if (!isNetworkException)
                    {
                        if (await TryHandleRateLimitResponseAsync(request, webRequest, token))
                        {
                            continue;
                        }
                    }

                    return result;
                }
            }
        }

        private async Task<DAResult<T>> TryParseResponse<T>(string text, DARequest request, UnityHttpClient webRequest)
        {
            DAResult<T> result = new DAResult<T>();
            int state;

            DAResult<WebError> figmaApiError = await DAJson.FromJsonAsync<WebError>(text);
            bool isRequestError = webRequest.result != WR_Result.Success;
            string requestResult = webRequest.result.ToString();

            if (figmaApiError.Object.err != null)
            {
                state = 1;
                result.Success = false;
                result.Error = ApplyRateLimitDetails(figmaApiError.Object, webRequest);
            }
            else if (isRequestError)
            {
                result.Success = false;

                if (webRequest.error.Contains("SSL"))
                {
                    state = 2;
                    result.Error = ApplyRateLimitDetails(new WebError(909, text), webRequest);
                }
                else
                {
                    state = 3;
                    result.Error = ApplyRateLimitDetails(new WebError((int)GetResponseCode(webRequest), webRequest.error), webRequest);
                }
            }
            else if (text.Contains("<pre>Cannot GET "))
            {
                state = 4;
                result.Error = ApplyRateLimitDetails(new WebError(404, text), webRequest);
            }
            else
            {
                DAResult<T> obj = await DAJson.FromJsonAsync<T>(text);

                if (obj.Success)
                {
                    state = 5;
                    result.Success = true;
                    result.Object = obj.Object;

                    if (request.Name == RequestName.Project)
                    {
                        monoBeh.ProjectCacher.Cache(obj.Object);
                    }
                }
                else
                {
                    state = 6;
                    result.Success = false;
                    result.Error = ApplyRateLimitDetails(obj.Error, webRequest);
                }
            }

            Debug.Log(FcuLocKey.log_request_sender_try_parse_state.Localize(state));

            return result;
        }

        private async Task<bool> TryHandleRateLimitResponseAsync(DARequest request, UnityHttpClient webRequest, CancellationToken token)
        {
            if (webRequest == null || GetResponseCode(webRequest) != (long)RateLimitStatusCode)
            {
                return false;
            }

            if (TryGetRetryAfter(webRequest, out TimeSpan waitTime) == false || waitTime <= TimeSpan.Zero)
            {
                return false;
            }


            string rateLimitDetails = BuildRateLimitDetails(webRequest);

            bool shouldShowWindow = waitTime > RateLimitWindowThreshold && monoBeh.EditorDelegateHolder.ShowRateLimitWindow != null;

            if (shouldShowWindow)
            {
                RateLimitWindowResult windowResult = await ShowRateLimitWindowAsync(waitTime, rateLimitDetails, token);

                if (windowResult.ShouldRetry)
                {
                    return true;
                }

                return false;
            }

            int seconds = Math.Max(1, (int)Math.Ceiling(waitTime.TotalSeconds));
            Debug.Log(FcuLocKey.log_api_waiting.Localize(seconds));
            await Task.Delay(waitTime, token);
            return true;
        }


        private async Task<RateLimitWindowResult> ShowRateLimitWindowAsync(
            TimeSpan waitTime, 
            string rateLimitDetails, 
            CancellationToken token)
        {
            RateLimitWindowData data = new RateLimitWindowData
            {
                WaitSeconds = waitTime.TotalSeconds,
                RateLimitDetails = rateLimitDetails,
                CreatedAtUtc = DateTime.UtcNow,
            };

            RateLimitWindowResult result = default;
            monoBeh.EditorDelegateHolder.ShowRateLimitWindow(data, output => result = output);

            while (result.IsDefault)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(200, token);
            }

            return result;
        }

        private static bool TryGetRetryAfter(UnityHttpClient webRequest, out TimeSpan waitTime)
        {
            waitTime = TimeSpan.Zero;

            if (webRequest == null)
            {
                return false;
            }

            string retryAfter = webRequest.GetResponseHeader("Retry-After");

            if (string.IsNullOrWhiteSpace(retryAfter) == false)
            {
                if (double.TryParse(retryAfter, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds) && seconds > 0d)
                {
                    waitTime = TimeSpan.FromSeconds(seconds);
                    return true;
                }

                if (DateTime.TryParse(retryAfter, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime retryDateUtc))
                {
                    TimeSpan difference = retryDateUtc - DateTime.UtcNow;

                    if (difference.TotalSeconds > 0d)
                    {
                        waitTime = difference;
                        return true;
                    }
                }
            }

            return false;
        }

        private static long GetResponseCode(UnityHttpClient webRequest)
        {
            if (webRequest == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt64(webRequest.responseCode);
            }
            catch
            {
                return 0;
            }
        }

        private static string BuildEndpointDetails(UnityHttpClient webRequest)
        {
            string url = webRequest.RequestUrl;
            string path = url;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                path = uri.AbsolutePath;
            }

            return $"{path ?? "Unknown endpoint"}";
        }

        private WebError ApplyRateLimitDetails(WebError error, UnityHttpClient webRequest)
        {
            if (webRequest == null || GetResponseCode(webRequest) != (long)RateLimitStatusCode)
            {
                return error;
            }

            if (error.status == 0)
            {
                error.status = (int)GetResponseCode(webRequest);
            }

            string rateLimitDetails = BuildRateLimitDetails(webRequest);

            if (string.IsNullOrWhiteSpace(rateLimitDetails) == false)
            {
                if (string.IsNullOrWhiteSpace(error.err))
                {
                    error.err = rateLimitDetails;
                }
                else
                {
                    error.err = $"{error.err}\n\n{rateLimitDetails}";
                }
            }

            if (error.err == null || error.err == "")
            {
                Debug.LogError(FcuLocKey.log_request_sender_error_field_empty.Localize(nameof(error.err), webRequest.result));
            }

            return error;
        }

        private string BuildRateLimitDetails(UnityHttpClient webRequest)
        {
            string retryAfter = FormatRetryAfter(webRequest.GetResponseHeader("Retry-After"));
            string planTier = webRequest.GetResponseHeader("X-Figma-Plan-Tier");
            string rateLimitType = webRequest.GetResponseHeader("X-Figma-Rate-Limit-Type");
            string upgradeLink = webRequest.GetResponseHeader("X-Figma-Upgrade-Link");
            int endpointTier = FigmaEndpointTierResolver.GetTier(webRequest?.RequestUrl);
            string endpointDetails = BuildEndpointDetails(webRequest);

            StringBuilder builder = new StringBuilder();

            AppendRateLimitLine(builder, "Endpoint", endpointDetails);
            AppendRateLimitLine(builder, "Tier", endpointTier.ToString());
            AppendRateLimitLine(builder, "Retry-After", retryAfter);
            AppendRateLimitLine(builder, "Plan Tier", planTier);
            AppendRateLimitLine(builder, "Rate Limit Type", rateLimitType);
            AppendRateLimitLine(builder, "Upgrade Link", upgradeLink);

            return builder.ToString().Trim();
        }

        private static string FormatRetryAfter(string retryAfterRaw)
        {
            if (string.IsNullOrWhiteSpace(retryAfterRaw))
            {
                return null;
            }

            if (double.TryParse(retryAfterRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds) && seconds > 0)
            {
                double value = seconds;
                string unit = "seconds";
                string originalSeconds = FormatNumber(seconds);

                if (value > 120)
                {
                    value /= 60d;
                    unit = "minutes";

                    if (value > 120)
                    {
                        value /= 60d;
                        unit = "hours";

                        if (value > 72)
                        {
                            value /= 24d;
                            unit = "days";
                        }
                    }
                }

                string formattedValue = value >= 100
                    ? Math.Round(value).ToString(CultureInfo.InvariantCulture)
                    : value.ToString("0.##", CultureInfo.InvariantCulture);

                return $"{formattedValue} {unit} ({originalSeconds} seconds)";
            }

            return retryAfterRaw;
        }

        private static string FormatNumber(double number)
        {
            return number % 1 == 0
                ? Math.Round(number).ToString(CultureInfo.InvariantCulture)
                : number.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void AppendRateLimitLine(StringBuilder builder, string label, string value)
        {
            if (builder == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            builder.AppendLine($"{label}: {value}");
        }

        private static DAResult<T> CreateErrorResult<T>(int status, string message, Exception exception = null)
        {
            return new DAResult<T>
            {
                Success = false,
                Error = new WebError(status, message, exception)
            };
        }

        private void LogRequestError(DARequest request, WebError error, string customErrorText)
        {
            string statusText = error.status != 0 ? $"Status: {error.status}" : string.Empty;

            string baseMessage = string.IsNullOrWhiteSpace(error.err) ? statusText : $"{error.err}\n{statusText}";

            string message;
            if (!string.IsNullOrWhiteSpace(customErrorText))
            {
                message = $"{customErrorText}\n{baseMessage}";
            }
            else
            {
                message = baseMessage;
            }

            Debug.LogError(message.Trim());
        }

        private async Task UpdateRequestProgressBar(UnityHttpClient webRequest)
        {
            while (webRequest.isDone == false)
            {
                if (webRequest.downloadProgress == 0 || webRequest.downloadedBytes == 0)
                {
                    if (pbarProgress < 1f)
                    {
                        pbarProgress += 0.01f;
                    }
                    else
                    {
                        pbarProgress = 0;
                    }

                    pbarBytes += 100;
                }
                else
                {
                    pbarProgress = webRequest.downloadProgress;
                    pbarBytes = webRequest.downloadedBytes;
                }

                await Task.Yield();
            }
        }

        private async Task MoveRequestProgressBarToEnd()
        {
            float left = 1f - pbarProgress;

            int steps = 10;
            float stepIncrement = left / steps;

            for (int i = 0; i < steps; i++)
            {
                pbarProgress += stepIncrement;
                await Task.Yield();
            }

            pbarProgress = 0f;
            pbarBytes = 0f;
        }
    }
}
