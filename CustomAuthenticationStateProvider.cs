  public class CustomAuthenticationStateProvider : AuthenticationStateProvider
  {
      private readonly ISessionStorageService _sessionStorage;
      private readonly HttpClient _httpClient;
      private readonly string _tokenKey = "authToken";

      public CustomAuthenticationStateProvider(ISessionStorageService sessionStorage, HttpClient httpClient)
      {
          _sessionStorage = sessionStorage;
          _httpClient = httpClient;
      }

      public override async Task<AuthenticationState> GetAuthenticationStateAsync()
      {
          var token = await _sessionStorage.GetItemAsync<string>(_tokenKey);

          if (string.IsNullOrEmpty(token))
          {
              return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
          }

          _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

          var claims = ParseClaimsFromJwt(token);
          var identity = new ClaimsIdentity(claims, "jwt");
          var user = new ClaimsPrincipal(identity);

          return new AuthenticationState(user);
      }

      public async Task MarkUserAsAuthenticated(string token)
      {
          await _sessionStorage.SetItemAsync(_tokenKey, token);
          var claims = ParseClaimsFromJwt(token);
          var identity = new ClaimsIdentity(claims, "jwt");
          var user = new ClaimsPrincipal(identity);
          NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
      }

      public async Task MarkUserAsLoggedOut()
      {
          await _sessionStorage.RemoveItemAsync(_tokenKey);
          var identity = new ClaimsIdentity();
          var user = new ClaimsPrincipal(identity);
          NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
      }

      private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
      {
          var handler = new JwtSecurityTokenHandler();
          var jwt = handler.ReadJwtToken(token);
          return jwt.Claims;
      }
     

  }
