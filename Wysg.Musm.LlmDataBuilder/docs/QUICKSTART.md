# LLM Data Builder - Quick Start Guide

## What is LLM Data Builder?

A desktop tool for creating training datasets for Large Language Models. It helps you collect question-answer pairs along with metadata in a structured JSON format.

## 5-Minute Quick Start

### 1. Launch the Application
- Open `Wysg.Musm.LlmDataBuilder.exe`
- The main window will appear with a dark-themed split-panel interface
- Use the "Always on Top" checkbox in the status bar to keep the window visible while working with other apps

### 2. Create Your First Record

**Left Panel - Data Entry:**
- **Input**: Enter your question or prompt
- **Output**: Enter the expected answer
- **Applied Prompt Numbers**: (Optional) Enter numbers like `1,2,3`

**Right Panel - Prompt:**
- Edit your master prompt template if needed

### 3. Save
- Click the **Save** button (green border)
- Your data is appended to `data.json`
- The prompt is saved to `prompt.txt`
- Data entry fields are automatically cleared

### 4. Repeat
- Continue adding more records
- The status bar shows your total record count

## Key Features

? **Dark Theme** - Modern UI that's easy on the eyes
? **Always on Top** - Keep window visible while multitasking
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
    "input": "What is the capital of France?",
    "output": "Paris",
    "protoOutput": "",
    "appliedPromptNumbers": [1, 2]
  }
]
```

### prompt.txt (in app directory)
```
Your master prompt template here...
```

## Button Guide

| Button | Action | Visual Cue |
|--------|--------|-----------|
| **Save** | Save current record to JSON and update prompt file | Green border |
| **Get Proto Result** | (Coming soon) Generate output from LLM server | Yellow border |
| **Clear Data Fields** | Reset all data fields except prompt | Red border |

## Window Controls

| Control | Location | Action |
|---------|----------|--------|
| **Always on Top** | Status bar (top-right) | Check to keep window above all others |

## Tips

?? **Input and Output are required** - You'll get a warning if they're empty
?? **Prompt persists** - The prompt field is never cleared automatically
?? **Comma-separated numbers** - Use format like `1,2,3` for prompt numbers
?? **JSON is formatted** - The output file is readable and properly indented
?? **Always on Top** - Useful when referencing other applications
?? **Dark theme** - Reduces eye strain during extended use

## Next Steps

- Read the full [README.md](README.md) for complete documentation
- Check the [UI Reference](UI_REFERENCE.md) for detailed interface information
- Review the [Data Schema](DATA_SCHEMA.md) for JSON structure details
- Check the [API Integration Guide](API_INTEGRATION.md) when implementing LLM server connection

## Need Help?

- Status bar shows current operation status
- Red text indicates errors
- Message boxes guide you through validation issues
- All operations have confirmation dialogs where appropriate
- Color-coded button borders indicate function (green=safe, red=destructive, yellow=caution)

---

**Remember**: This tool is designed for iterative data collection. Start small, build your dataset gradually, and refine your prompts as you go!
