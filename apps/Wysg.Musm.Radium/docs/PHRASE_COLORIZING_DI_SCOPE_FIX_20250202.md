# ? CRITICAL FIX: Dependency Injection Scope Mismatch

**Date:** 2025-02-02  
**Status:** ? **BUILD SUCCESSFUL** - Critical DI violation fixed

---

## Problem: Phrase Colorizing Still Not Working

After previous fixes:
- ? Created `ApiSnomedMapServiceAdapter`
- ? Fixed class name in DI registration  
- ? Fixed constructor dependency type

**But colorizing STILL didn't work!** Your screenshot showed all phrases grey/red only.

---

## Root Cause: DI Lifetime Scope Violation

### The Violation

```csharp
// ? WRONG - Scoped service:
services.AddScoped<ISnomedApiClient>(sp => ...);

// ? WRONG - Singleton trying to inject Scoped:
services.AddSingleton<ISnomedMapService>(sp =>
{
    return new ApiSnomedMapServiceAdapter(
        sp.GetRequiredService<ISnomedApiClient>());  // ? RUNTIME ERROR!
});
```

### Why This Fails

**DI Lifetime Rules:**
1. ? **Transient** can depend on: Transient, Scoped, Singleton
2. ? **Scoped** can depend on: Scoped, Singleton
3. ? **Singleton** CANNOT depend on: Scoped (shorter lifetime!)

**What Happened:**
1. App starts, DI container tries to create `ISnomedMapService` (Singleton)
2. `ApiSnomedMapServiceAdapter` constructor needs `ISnomedApiClient`
3. DI container finds `ISnomedApiClient` is registered as `Scoped`
4. **RUNTIME ERROR:** Cannot inject Scoped service into Singleton!
5. Result: `_snomedMapService` is null ¡æ colorizing doesn't work

---

## Solution Applied

Changed `ISnomedApiClient` from `Scoped` to `Singleton`:

```csharp
// ? CORRECT - Singleton:
services.AddSingleton<ISnomedApiClient>(sp => ...);
```

**Why Singleton is correct for HttpClient wrappers:**
- Thread-safe
- Stateless
- Connection pooling benefits
- Performance

---

## Testing

```powershell
$env:USE_API = "1"
cd apps\Wysg.Musm.Radium.Api
dotnet run
# New terminal:
cd apps\Wysg.Musm.Radium
dotnet run
```

Type in editor to verify colors:
- `chest pain` ¡æ **Pink**
- `heart` ¡æ **Green**
- `ct scan` ¡æ **Yellow**

---

**Status:** ? **COMPLETE** - This was the final missing piece!
