# Quick Reference: API Parameter Updates (v1.3.2)

## What Changed

The API integration now matches your actual curl command format with these additional parameters:
- ? `language` - Language code (default: "en")
- ? `strictness` - Evaluation level 1-5 (default: 4)
- ? Auth token - Updated to "change-me"

## Updated API Request

**Your curl command:**
```bash
curl -X POST http://192.168.111.79:8081/v1/evaluations \
    -H 'Authorization: Bearer change-me' \
    -H 'Content-Type: application/json' \
    -d '{
          "prompt": "{prompt text}",
          "candidate_text": "{input}",
          "language": "en",
          "strictness": 4
        }'
```

**Now the app sends:**
```json
{
  "prompt": "Proofread",
  "candidate_text": "The launch were sucessful",
  "language": "en",
  "strictness": 4
}
```

## Configuration

### Update your api_config.json

**Before:**
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "local-dev-token",
  "defaultPrompt": "Proofread"
}
```

**After (with new options):**
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "change-me",
  "defaultPrompt": "Proofread",
  "language": "en",
  "strictness": 4
}
```

## Strictness Levels

Choose the right level for your needs:

| Level | When to Use | Example |
|-------|-------------|---------|
| **1** | Quick checks only | Draft review |
| **2** | Basic corrections | Casual writing |
| **3** | Standard proofreading | General content |
| **4** | Thorough review **¡ç Default** | Professional writing |
| **5** | Maximum scrutiny | Academic papers |

## Language Codes

Common language codes:
- `"en"` - English (default)
- `"ko"` - Korean
- `"ja"` - Japanese
- `"zh"` - Chinese
- `"es"` - Spanish
- `"fr"` - French
- `"de"` - German

## Quick Setup

### Step 1: Update Config File
1. Open `api_config.json` in your app directory
2. Change `"authToken": "local-dev-token"` to `"authToken": "change-me"`
3. Add `"language": "en"` (if not present)
4. Add `"strictness": 4` (if not present)

### Step 2: Restart App
- Close and reopen the application
- New settings will be loaded automatically

### Step 3: Test
1. Enter test text in Input field
2. Click "Get Proto Result"
3. Check if API responds successfully

## Common Use Cases

### English with Maximum Strictness
```json
{
  "language": "en",
  "strictness": 5
}
```

### Korean with Moderate Checking
```json
{
  "language": "ko",
  "strictness": 3
}
```

### Quick English Check
```json
{
  "language": "en",
  "strictness": 1
}
```

## Troubleshooting

### Issue: 401 Unauthorized
**Cause**: Wrong auth token  
**Fix**: Change `"authToken"` to `"change-me"` in `api_config.json`

### Issue: Invalid strictness error
**Cause**: Strictness value outside 1-5 range  
**Fix**: Ensure `"strictness"` is between 1 and 5

### Issue: Wrong language results
**Cause**: Incorrect language code  
**Fix**: Use proper ISO language code (e.g., "en", "ko", "ja")

## Testing Different Strictness Levels

Try the same text with different strictness to see the difference:

**Test Text:** "The launch were sucessful and we was happy"

**Strictness 1-2:** May catch only major errors  
**Strictness 3-4:** Catches grammar and spelling  
**Strictness 5:** Catches everything + style suggestions

## Default Behavior

If you don't update your config:
- ? `language` defaults to "en"
- ? `strictness` defaults to 4
- ? Everything continues to work

No action required for existing users!

## API Response

The response format remains the same:
```json
{
  "proofread_text": "The launch was successful",
  "status": "completed",
  "issues": [
    {
      "category": "grammar",
      "suggestion": "were ¡æ was",
      "severity": "medium",
      "confidence": 0.95
    }
  ],
  "model_name": "nemotron-4-340b-instruct",
  "latency_ms": 1200
}
```

## See Also

- [README.md](README.md) - Complete documentation
- [CHANGELOG.md](CHANGELOG.md) - Version 1.3.2 details
- [API_INTEGRATION.md](API_INTEGRATION.md) - API integration guide

---

**Version**: 1.3.2  
**Date**: 2025-01-24  
**Status**: ? Ready to use  
**Backward Compatible**: ? Yes
