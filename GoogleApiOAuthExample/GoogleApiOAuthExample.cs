using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.YouTubeAnalytics.v1;

namespace GoogleApiOAuthExample
{
    // Refer to https://developers.google.com/identity/protocols/OAuth2 for the official overview of
    // OAuth and the Google APIs. These are some quick code samples to get a valid OAuth for an
    // Installed App, as per https://developers.google.com/identity/protocols/OAuth2InstalledApp
    public class GoogleApiOAuthExample
    {
        // All of the below details can be found by visiting https://console.developers.google.com/project
        // and then selecting your project followed by Credentials. If there are no details there then you need to create
        // a new client ID. Refer to README.md for details
        private const string ClientId = "CLIENT_ID";
        private const string ClientSecret = "CLIENT_SECRET";
        private const string RedirectUri = "REDIRECT_URI";

        // This will generate a url for a user to visit and grant us access to the APIs we request.
        // If they grant us access then they will be given an authorization code to give back to us.
        // We can then use this authorization code to request an OAuth token
        public string GenerateAuthorizationUrl()
        {
            var sb = new StringBuilder("https://accounts.google.com/o/oauth2/auth?");

            // This is a space-delimited list of all the APIs we want to be able to access on behalf of the user.
            // We need to make sure we url-encode our requested scopes
            sb.Append("scope=");
            sb.Append(WebUtility.UrlEncode("https://www.googleapis.com/auth/yt-analytics.readonly"));
            sb.Append(WebUtility.UrlEncode(" https://www.googleapis.com/auth/yt-analytics-monetary.readonly"));

            sb.Append("&redirect_uri=");
            sb.Append(RedirectUri);

            sb.Append("&response_type=code");

            sb.Append("&client_id=");
            sb.Append(ClientId);

            return sb.ToString();
        }

        // Once we have the authorization code from the user we can request our OAuth token from Google.
        // The OAuth token gives us permission to make API calls on behalf of the user.
        // The response from the POST request will be JSON containing our OAuth token and refresh token:
        //
        // {
        //   "access_token":"ACCESS_TOKEN",
        //   "expires_in":3600,
        //   "token_type":"Bearer",
        //   "refresh_token":"REFRESH_TOKEN"
        // }
        public async Task<string> GetOAuthToken(string authorizationCode)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://www.googleapis.com/oauth2/v3/token")
            };

            var content = new StringBuilder();

            content.Append("code=");
            content.Append(authorizationCode);

            content.Append("&client_id=");
            content.Append(ClientId);

            content.Append("&client_secret=");
            content.Append(ClientSecret);

            content.Append("&redirect_uri=");
            content.Append(RedirectUri);

            content.Append("&grant_type=authorization_code");

            var requestContent = new StringContent(content.ToString());
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.PostAsync("", requestContent);

            return await response.Content.ReadAsStringAsync();
        }

        // You may have noticed that the JSON we received above had an expiry time.
        // When our OAuth token expires we will need to request a new one.
        // Luckily we don't need the client to grant us permission again, we just need the refresh token from above.
        // We can make a slightly modified call to the same url, this time posting our refresh token.
        // The refresh token will always be valid unless a user revokes the permissions they originally granted us.
        // The response is also very similar, but this time without a refresh token:
        //
        // {
        //   "access_token":"ACCESS_TOKEN",
        //   "expires_in":3920,
        //   "token_type":"Bearer",
        // }
        public async Task<string> GetFreshOAuthToken(string refreshToken)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://www.googleapis.com/oauth2/v3/token")
            };

            var content = new StringBuilder();

            content.Append("client_id=");
            content.Append(ClientId);

            content.Append("&client_secret=");
            content.Append(ClientSecret);

            content.Append("&refresh_token=");
            content.Append(refreshToken);

            content.Append("&grant_type=refresh_token");

            var requestContent = new StringContent(content.ToString());
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.PostAsync("", requestContent);

            return await response.Content.ReadAsStringAsync();
        }

        // When we have a valid OAuth token we can start using the Google APIs.
        // The below example creates a YouTube analytics service and requests the views from
        // a particular time period for the current user's channel
        public async Task MakeAnalyticReportRequest(string oauthToken)
        {
            var service = new YouTubeAnalyticsService();

            // Set the authorization header on all of our requests using our OAuth token
            service.HttpClient.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse("Bearer " + oauthToken);

            var query = service.Reports.Query("channel==MINE", "2013-05-07", "2013-05-07", "views");

            var result = await query.ExecuteAsync();
        }
    }
}
