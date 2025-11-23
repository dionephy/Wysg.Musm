# User Settings API Documentation

## Overview

The User Settings API provides secure access to user-specific configuration stored in the `radium.user_setting` table. This API replaces direct database access from the desktop client for enhanced security.

## Base URL

```
https://your-api-domain.com/api/accounts/{accountId}/settings
```

## Authentication

All endpoints require Firebase JWT authentication. Include the Firebase ID token in the `Authorization` header:

```
Authorization: Bearer YOUR_FIREBASE_TOKEN
```

## Endpoints

### 1. Get User Settings

Retrieves user settings for a specific account.

**Request:**
```http
GET /api/accounts/{accountId}/settings
Authorization: Bearer {firebaseToken}
```

**Response:** `200 OK`
```json
{
  "accountId": 123,
  "settingsJson": "{\"remove_excessive_blanks\":true,...}",
  "updatedAt": "2025-01-22T10:30:00Z",
  "rev": 5
}
```

**Error Responses:**
- `404 Not Found` - Settings not found for account
- `401 Unauthorized` - Missing or invalid Firebase token
- `403 Forbidden` - User doesn't have access to this account
- `400 Bad Request` - Invalid account ID
- `500 Internal Server Error` - Server error

---

### 2. Create or Update User Settings

Creates new settings or updates existing settings for an account.

**Request:**
```http
PUT /api/accounts/{accountId}/settings
Authorization: Bearer {firebaseToken}
Content-Type: application/json

{
  "settingsJson": "{\"theme\":\"dark\",\"language\":\"en\"}"
}
```

**Response:** `200 OK`
```json
{
  "accountId": 123,
  "settingsJson": "{\"theme\":\"dark\",\"language\":\"en\"}",
  "updatedAt": "2025-01-22T10:35:00Z",
  "rev": 6
}
```

**Request Body:**
- `settingsJson` (required): Valid JSON string containing user settings

**Error Responses:**
- `400 Bad Request` - Invalid JSON format or missing required fields
- `401 Unauthorized` - Missing or invalid Firebase token
- `403 Forbidden` - User doesn't have access to this account
- `500 Internal Server Error` - Server error

---

### 3. Delete User Settings

Deletes user settings for an account.

**Request:**
```http
DELETE /api/accounts/{accountId}/settings
Authorization: Bearer {firebaseToken}
```

**Response:** `204 No Content`

**Error Responses:**
- `404 Not Found` - Settings not found for account
- `401 Unauthorized` - Missing or invalid Firebase token
- `403 Forbidden` - User doesn't have access to this account
- `400 Bad Request` - Invalid account ID
- `500 Internal Server Error` - Server error

---

## Settings JSON Schema

The `settingsJson` field can contain any valid JSON. For reportify settings, the following schema is commonly used:

```json
{
  "remove_excessive_blanks": true,
  "remove_excessive_blank_lines": true,
  "capitalize_sentence": true,
  "ensure_trailing_period": true,
  "space_before_arrows": false,
  "space_after_arrows": true,
  "space_before_bullets": false,
  "space_after_bullets": true,
  "space_after_punctuation": true,
  "normalize_parentheses": true,
  "space_number_unit": true,
  "collapse_whitespace": true,
  "number_conclusion_paragraphs": true,
  "indent_continuation_lines": true,
  "number_conclusion_lines_on_one_paragraph": false,
  "capitalize_after_bullet_or_number": false,
  "consider_arrow_bullet_continuation": false,
  "defaults": {
    "arrow": "-->",
    "conclusion_numbering": "1.",
    "detailing_prefix": "-",
    "differential_diagnosis": "DDx:"
  }
}
```

However, any valid JSON structure can be stored.

---

## Security

### Authorization

The API enforces the following security measures:

1. **Firebase Authentication Required**: All endpoints require a valid Firebase JWT token
2. **Account Ownership Verification**: Users can only access settings for their own account
3. **No SQL Injection**: All queries use parameterized statements
4. **JSON Validation**: Settings JSON is validated before storage

### Best Practices

1. **Never expose Firebase tokens**: Tokens should only be used in HTTPS requests
2. **Token refresh**: Implement token refresh logic in your client
3. **Error handling**: Don't expose sensitive information in error messages
4. **Rate limiting**: Consider implementing rate limiting in production

---

## Migration from Direct Database Access

### Before (Desktop Client ¡æ Database)

```csharp
// Old: Direct PostgreSQL connection (INSECURE)
const string sql = "SELECT settings_json::text FROM radium.reportify_setting WHERE account_id=@aid";
await using var con = CreateConnection();
await using var cmd = new NpgsqlCommand(sql, con);
// ... execute query
```

### After (Desktop Client ¡æ API ¡æ Database)

```csharp
// New: API call with authentication (SECURE)
var request = new HttpRequestMessage(HttpMethod.Get, 
    $"https://api.example.com/api/accounts/{accountId}/settings");
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", firebaseToken);
var response = await httpClient.SendAsync(request);
var settings = await response.Content.ReadFromJsonAsync<UserSettingDto>();
```

---

## Examples

### Example 1: Get Settings with HttpClient (C#)

```csharp
public async Task<UserSettingDto?> GetUserSettingsAsync(long accountId, string firebaseToken)
{
    using var client = new HttpClient();
    client.BaseAddress = new Uri("https://your-api-domain.com");
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", firebaseToken);
    
    var response = await client.GetAsync($"/api/accounts/{accountId}/settings");
    
    if (response.StatusCode == HttpStatusCode.NotFound)
        return null;
    
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<UserSettingDto>();
}
```

### Example 2: Update Settings with HttpClient (C#)

```csharp
public async Task<UserSettingDto> UpdateUserSettingsAsync(
    long accountId, 
    string settingsJson, 
    string firebaseToken)
{
    using var client = new HttpClient();
    client.BaseAddress = new Uri("https://your-api-domain.com");
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", firebaseToken);
    
    var request = new UpdateUserSettingRequest { SettingsJson = settingsJson };
    var response = await client.PutAsJsonAsync(
        $"/api/accounts/{accountId}/settings", 
        request);
    
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<UserSettingDto>();
}
```

### Example 3: Using REST Client / curl

```bash
# Get settings
curl -X GET "https://your-api-domain.com/api/accounts/123/settings" \
  -H "Authorization: Bearer YOUR_FIREBASE_TOKEN"

# Update settings
curl -X PUT "https://your-api-domain.com/api/accounts/123/settings" \
  -H "Authorization: Bearer YOUR_FIREBASE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"settingsJson":"{\"theme\":\"dark\"}"}'

# Delete settings
curl -X DELETE "https://your-api-domain.com/api/accounts/123/settings" \
  -H "Authorization: Bearer YOUR_FIREBASE_TOKEN"
```

---

## Testing

Use the provided `test-user-settings.http` file with Visual Studio Code REST Client extension or similar tools.

**Steps:**
1. Obtain a Firebase authentication token
2. Replace `YOUR_FIREBASE_TOKEN_HERE` in the test file
3. Execute requests

---

## Deployment Checklist

- [ ] Run database migration script
- [ ] Deploy API with new endpoints
- [ ] Update desktop client to use API instead of direct database access
- [ ] Test authentication flow
- [ ] Verify CORS settings for production
- [ ] Configure rate limiting (optional)
- [ ] Set up monitoring and logging
- [ ] Update client-side error handling

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-22 | Initial release with table rename (reportify_setting ¡æ user_setting) and API endpoints |

---

## Support

For issues or questions, please contact the development team or create an issue in the project repository.
