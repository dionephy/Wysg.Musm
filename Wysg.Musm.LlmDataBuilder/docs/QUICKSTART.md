# LLM Data Builder - Quick Start Guide

## What is LLM Data Builder?

A desktop tool for creating training datasets for Large Language Models. It helps you collect question-answer pairs along with metadata in a structured JSON format. Now includes API integration to automatically generate proto results from a proofreading service!

## 5-Minute Quick Start

### 1. Launch the Application
- Open `Wysg.Musm.LlmDataBuilder.exe`
- The main window will appear with a dark-themed split-panel interface
- Use the "Always on Top" checkbox in the status bar to keep the window visible while working with other apps

### 2. (Optional) Configure API
If you want to use the "Get Proto Result" feature:
- Copy `api_config.json.sample` to `api_config.json`
- Edit the file with your API settings:
  ```json
  {
    "apiUrl": "http://192.168.111.79:8081",
    "authToken": "local-dev-token",
    "defaultPrompt": "Proofread"
  }
  ```
- Restart the application

### 3. Create Your First Record

**Left Panel - Data Entry:**
- **Input**: Enter your question or text to proofread (becomes `candidate_text` in API)
- **Click "Get Proto Result"**: ? NEW! Calls the API to generate proofread text automatically
- **Output**: Enter the expected answer (or copy from Proto Output)
- **Applied Prompt Numbers**: (Optional) Enter numbers like `1,2,3`

**Right Panel - Prompt:**
- Edit your master prompt template (becomes `prompt` parameter in API, default: "Proofread")

### 4. Using Get Proto Result (NEW!)

The "Get Proto Result" button connects to an external proofreading API:

1. **Enter Input**: Type text like "The launch were sucessful"
2. **Set Prompt**: Ensure prompt is set (e.g., "Proofread")
3. **Click "Get Proto Result"**:
   - Application calls API with your input and prompt
   - Proto Output field is automatically filled with the result
   - Any issues or suggestions are displayed in a dialog
   - Status bar shows model name, latency, and issue count

**Example API Call:**
```
Input: "The launch were sucessful"
Prompt: "Proofread"
¡æ API Result: "The launch was successful"
¡æ Issues: 1 auto-correction found
```

### 5. Save
- Click the **Save** button (green border)
- Your data is appended to `data.json`
- The prompt is saved to `prompt.txt`
- Data entry fields are automatically cleared

### 6. Repeat
- Continue adding more records
- The status bar shows your total record count

## Key Features

? **Dark Theme** - Modern UI that's easy on the eyes
? **Always on Top** - Keep window visible while multitasking
? **API Integration** - Auto-generate proto results from proofreading API
? **Simple Interface** - Clear, focused UI for data entry
? **Auto-Save** - Data and prompts saved together
? **Validation** - Prevents empty or invalid entries
? **Persistent** - All data saved to local JSON file
? **Record Count** - Always know how many records you have

## File Output

### data.json (in app directory)
```json
[
  {
    "input": "The launch were sucessful",
    "output": "The launch was successful",
    "protoOutput": "The launch was successful",
    "appliedPromptNumbers": [1, 2]
  }
]
```

### prompt.txt (in app directory)
```
Proofread
```

### api_config.json (optional, in app directory)
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "local-dev-token",
  "defaultPrompt": "Proofread"
}
```

## Button Guide

| Button | Action | Visual Cue |
|--------|--------|-----------|
| **Save** | Save current record to JSON and update prompt file | Green border |
| **Get Proto Result** | ? Call API to generate proofread text | Yellow border |
| **Clear Data Fields** | Reset all data fields except prompt | Red border |

## Window Controls

| Control | Location | Action |
|---------|----------|--------|
| **Always on Top** | Status bar (top-right) | Check to keep window above all others |

## API Integration

### How It Works
1. You enter text in "Input" (e.g., "The launch were sucessful")
2. Set your "Prompt" (e.g., "Proofread")
3. Click "Get Proto Result"
4. Application sends:
   ```json
   POST /v1/evaluations
   {
     "prompt": "Proofread",
     "candidate_text": "The launch were sucessful"
   }
   ```
5. API responds with proofread text
6. "Proto Output" is automatically filled
7. Issues dialog shows corrections

### API Setup
**Default configuration works if your API is at:**
- URL: `http://192.168.111.79:8081`
- Token: `local-dev-token`

**To change settings:**
1. Copy `api_config.json.sample` to `api_config.json`
2. Edit the values
3. Restart application

### Troubleshooting API
- ? **Connection refused**: Make sure API server is running
- ? **401 Unauthorized**: Check `authToken` in config
- ? **Timeout**: Verify network connectivity and API URL
- ? **Success**: Status bar shows "API Success! Model: ..."

## Tips

?? **Input and Output are required** - You'll get a warning if they're empty
?? **Prompt persists** - The prompt field is never cleared automatically
?? **Comma-separated numbers** - Use format like `1,2,3` for prompt numbers
?? **JSON is formatted** - The output file is readable and properly indented
?? **Always on Top** - Useful when referencing other applications
?? **Dark theme** - Reduces eye strain during extended use
?? **API is optional** - You can manually enter Proto Output if you don't have API access
?? **Button disables during API call** - Prevents duplicate requests

## Workflow Examples

### With API (Recommended)
1. Enter Input: "The launch were sucessful"
2. Click "Get Proto Result" ¡æ Auto-fills Proto Output
3. Review result in Proto Output
4. Copy to Output if correct, or edit as needed
5. Click Save

### Without API (Manual)
1. Enter Input: "The launch were sucessful"
2. Manually enter Proto Output: "[N/A]" or leave empty
3. Enter Output: "The launch was successful"
4. Click Save

## Next Steps

- Read the full [README.md](README.md) for complete documentation
- Check the [UI Reference](UI_REFERENCE.md) for detailed interface information
- Review the [Data Schema](DATA_SCHEMA.md) for JSON structure details
- See [CHANGELOG.md](CHANGELOG.md) for version history and API integration details

## Need Help?

- Status bar shows current operation status
- Red text indicates errors
- Message boxes guide you through validation issues
- All operations have confirmation dialogs where appropriate
- Color-coded button borders indicate function (green=safe, red=destructive, yellow=caution)
- API errors include troubleshooting tips in the error dialog

---

**Remember**: This tool is designed for iterative data collection. Start small, build your dataset gradually, and refine your prompts as you go! Use the API integration to speed up your workflow by auto-generating proto results.
