# Quick Reference: Reportify Line Numbering & Capitalization

## New Features

### 1. Number Each Line on One Paragraph
?? **Location**: Settings ¡æ Reportify ¡æ Conclusion Numbering  
?? **Setting**: `number_conclusion_lines_on_one_paragraph`

**What it does**: Numbers each line separately instead of numbering paragraphs. Continuation lines (before blank line) are indented.

**Example:**
```
Input:
apple
banana

melon

Output:
1. Apple.
   Banana.

2. Melon.
```

---

### 2. Capitalize After Bullet or Number
?? **Location**: Settings ¡æ Reportify ¡æ Sentence Formatting  
?? **Setting**: `capitalize_after_bullet_or_number`

**What it does**: Capitalizes the first letter after bullets (`- `), numbers (`1. `), arrows (`--> `), and indentation.

**Example:**
```
Input:
1. apple
2. banana

Output:
1. Apple
2. Banana
```

---

## Quick Setup Guide

### For Line-Based Numbering
1. ? Number conclusion paragraphs
2. ? On one paragraph, number each line
3. ? Indent continuation lines
4. Click "Save Settings"

### For Enhanced Capitalization
1. ? Capitalize first letter
2. ? Also capitalize after bullet or number
3. Click "Save Settings"

### For Both (Recommended)
1. ? Number conclusion paragraphs
2. ? On one paragraph, number each line
3. ? Capitalize first letter
4. ? Also capitalize after bullet or number
5. ? Ensure trailing period
6. Click "Save Settings"

---

## Common Use Cases

### Use Case 1: Simple List
```
Input:  apple\nbanana\nmelon
Output: 1. Apple.\n2. Banana.\n3. Melon.
```

### Use Case 2: Grouped Items
```
Input:  apple\nbanana\n\nmelon\ngrape
Output: 1. Apple.\n   Banana.\n\n2. Melon.\n   Grape.
```

### Use Case 3: Mixed Formatting
```
Input:  - finding one\n- finding two
Output: - Finding one.\n- Finding two.
```

---

## Hint Buttons

Click **Hint** next to each option to see live examples:
- `number_conclusion_lines_on_one_paragraph` ¡æ Shows line-based numbering example
- `capitalize_after_bullet_or_number` ¡æ Shows capitalization example

---

## Default Behavior

**Both options disabled** = Original reportify behavior (paragraph numbering, basic capitalization)

---

## Compatibility

? Works with all existing reportify options  
? Persists across sessions (saved to database)  
? Syncs across devices for same account

---

## Quick Test

**Test Input:**
```
apple
banana

melon
```

**Enable both new options ¡æ Expected Output:**
```
1. Apple.
   Banana.

2. Melon.
```
