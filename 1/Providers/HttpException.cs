using System.Net;

namespace Providers;

public class HttpException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public HttpException(HttpResponseMessage response)
        : base(BuildMessage(response))
    {
        StatusCode = response.StatusCode;
    }

    private static string BuildMessage(HttpResponseMessage response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        var responseContent = response.Content.ReadAsStringAsync().Result;

        return
            $"HTTP request to '{response.RequestMessage?.RequestUri}' has failed with status code '{response.StatusCode}'." +
            $" Response content: {responseContent}";
    }
}