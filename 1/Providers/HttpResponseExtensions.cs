namespace Providers;

public static class HttpResponseExtensions
{
    public static HttpResponseMessage Validate(this HttpResponseMessage httpResponseMessage)
    {
        if (httpResponseMessage == null)
            throw new ArgumentNullException(nameof(httpResponseMessage));

        if (!httpResponseMessage.IsSuccessStatusCode)
            throw new HttpException(httpResponseMessage);

        return httpResponseMessage;
    }

    public static async Task<TResult> ReadContentAs<TResult>(this HttpResponseMessage response) where TResult : class
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        return (await response.Content.ReadAsStringAsync()).FromJson<TResult>();
    }
}