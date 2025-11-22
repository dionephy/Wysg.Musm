# ??? Quick Reference: Parallel Testing

## ?? Quick Start

### Default: Direct Database
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

### API Mode: Use Backend API
```powershell
# Terminal 1: Start API
cd apps\Wysg.Musm.Radium.Api
dotnet run

# Terminal 2: Run WPF with API mode
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

---

## ?? Environment Variables

| Variable | Values | Description |
|----------|--------|-------------|
| `USE_API` | `0` or `1` | Enable API mode (`1`) or Direct DB (`0`) |
| `RADIUM_API_URL` | URL | API base URL (default: `http://localhost:5205`) |

---

## ?? Check Current Mode

Look for in Debug output:
```
[DI] API Mode: ENABLED (via API)        ก็ API Mode
[DI] API Mode: DISABLED (direct DB)     ก็ Direct DB Mode
```

---

## ?? What Changes?

| Feature | Direct DB | API Mode |
|---------|-----------|----------|
| **Hotkeys** | `AzureSqlHotkeyService` | `ApiHotkeyServiceAdapter` |
| **Snippets** | `AzureSqlSnippetService` | `ApiSnippetServiceAdapter` |
| **Everything else** | No change | No change |

---

## ? Testing Checklist

- [ ] Start API (`dotnet run` in Radium.Api)
- [ ] Set `USE_API=1`
- [ ] Run WPF app
- [ ] Test hotkeys (create, edit, delete, toggle)
- [ ] Test snippets (create, edit, delete, toggle)
- [ ] Compare with Direct DB mode

---

## ?? Quick Troubleshooting

**API not working?**
```powershell
# Check if API is running
Invoke-WebRequest -Uri "http://localhost:5205/health"
```

**Switch back to Direct DB:**
```powershell
Remove-Item env:USE_API
# Restart app
```

---

## ?? Full Documentation

See: `PARALLEL_TESTING_GUIDE.md`

---

**Safe, flexible, no risk!** ??
