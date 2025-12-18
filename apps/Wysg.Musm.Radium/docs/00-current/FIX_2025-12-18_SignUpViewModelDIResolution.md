# FIX: SignUpViewModel DI Resolution Error

## Date
2025-12-18

## Summary
Fixed `InvalidOperationException` when clicking "Sign Up" button on the splash login window due to missing `AzureSqlCentralService` registration in the DI container.

## Problem
When pressing "Sign Up" in the splash window, the following error was thrown:
```
System.InvalidOperationException: Unable to resolve service for type 
'Wysg.Musm.Radium.Services.AzureSqlCentralService' while attempting to 
activate 'Wysg.Musm.Radium.ViewModels.SignUpViewModel'.
```

The `SignUpViewModel` depended on `AzureSqlCentralService` which was no longer registered in the DI container after the migration to use `RadiumApiClient` for all API communications.

## Root Cause
The `SplashLoginViewModel` and other view models had been migrated to use `RadiumApiClient` for API calls, but `SignUpViewModel` was still using the deprecated `AzureSqlCentralService` directly.

## Solution
Updated `SignUpViewModel` to use `RadiumApiClient` instead of `AzureSqlCentralService`:

### Changes Made

**apps/Wysg.Musm.Radium/ViewModels/SignUpViewModel.cs**:
1. Replaced `AzureSqlCentralService` dependency with `RadiumApiClient`
2. Updated constructor to accept `RadiumApiClient` instead of `AzureSqlCentralService`
3. Modified `OnSignUpAsync` method to:
   - Set Firebase auth token on `RadiumApiClient` after successful sign-up
   - Use `_apiClient.EnsureAccountAsync()` with `EnsureAccountRequest` DTO
4. Added proper error handling for HTTP-related exceptions:
   - `HttpRequestException` for connection failures
   - `TaskCanceledException` for timeouts
   - General `Exception` for other errors

## Code Changes

### Before
```csharp
public SignUpViewModel(IAuthService auth, AzureSqlCentralService central)
{
    _auth = auth;
    _central = central;
    // ...
}

// In OnSignUpAsync:
await _central.EnsureAccountAsync(result.UserId, result.Email, result.DisplayName);
```

### After
```csharp
public SignUpViewModel(IAuthService auth, RadiumApiClient apiClient)
{
    _auth = auth;
    _apiClient = apiClient;
    // ...
}

// In OnSignUpAsync:
_apiClient.SetAuthToken(result.IdToken);
await _apiClient.EnsureAccountAsync(new EnsureAccountRequest
{
    Uid = result.UserId,
    Email = result.Email,
    DisplayName = result.DisplayName ?? string.Empty
});
```

## Impact
- Sign-up functionality now works correctly from the splash login window
- Consistent API client usage across all view models
- Better error handling for network-related failures

## Testing
1. Launch the application
2. Click "Sign Up" button on splash screen
3. Sign-up window should open without errors
4. Complete sign-up form and submit
5. Account should be created successfully via the Radium API
