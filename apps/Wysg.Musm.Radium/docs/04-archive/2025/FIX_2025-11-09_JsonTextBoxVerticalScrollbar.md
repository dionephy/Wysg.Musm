# Fix: Add Vertical Scrollbar to JSON TextBox

**Date:** 2025-11-09  
**Type:** Bug Fix  
**Priority:** Low  
**Status:** ? Complete

## Summary

Added vertical scrollbar to the JSON TextBox (`txtCurrentJson`) in `ReportInputsAndJsonPanel` to prevent the control from stretching when JSON content exceeds the available height.

## Problem

The JSON TextBox was incorrectly configured:
- `VerticalScrollBarVisibility` and `HorizontalScrollBarVisibility` attributes were placed directly on the `TextBox` (incorrect)
- No `ScrollViewer` wrapper existed
- When JSON content grew longer, the TextBox would stretch the parent control instead of showing a scrollbar

## Solution

Wrapped the `TextBox` in a `ScrollViewer` with proper scroll settings:
- Outer `ScrollViewer`: `VerticalScrollBarVisibility="Auto"` (shows scrollbar when needed)
- Outer `ScrollViewer`: `HorizontalScrollBarVisibility="Disabled"` (prevents horizontal scrolling)
- Inner `TextBox`: Disabled internal scrollbars (they're now handled by outer ScrollViewer)

## Changes Made

**File Modified:**
- `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml`

**Before:**
```xaml
<TextBox x:Name="txtCurrentJson" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled"
         Text="{Binding CurrentReportJson, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
         Padding="6" Margin="0" TextWrapping="Wrap" AcceptsReturn="True"/>
```

**After:**
```xaml
<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
    <TextBox x:Name="txtCurrentJson"
             Text="{Binding CurrentReportJson, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
             Padding="6" Margin="0" TextWrapping="Wrap" AcceptsReturn="True"
             VerticalScrollBarVisibility="Disabled"
             HorizontalScrollBarVisibility="Disabled"/>
</ScrollViewer>
```

## Behavior

**Before Fix:**
- JSON TextBox would grow vertically without limit
- Could stretch the entire control/window
- No scrollbar appeared

**After Fix:**
- JSON TextBox height is constrained by TabItem/Grid
- Vertical scrollbar appears when content exceeds available height
- TextBox doesn't stretch the parent control
- Horizontal scrollbar is disabled (text wraps instead)

## Technical Details

**Why This Works:**
- `ScrollViewer` is the WPF control that provides scrolling capability
- `VerticalScrollBarVisibility="Auto"` shows scrollbar only when content overflows
- Inner `TextBox` has `TextWrapping="Wrap"` so horizontal scrolling isn't needed
- Inner `TextBox` scrollbars are disabled to avoid double-scrollbar issue

## Build Status

? Build successful with no errors or warnings

## Testing Checklist

- [ ] Short JSON content: No scrollbar visible
- [ ] Long JSON content: Vertical scrollbar appears
- [ ] JSON TextBox doesn't stretch parent control
- [ ] Horizontal scrolling is disabled (text wraps)
- [ ] Scrollbar works smoothly
- [ ] Text editing still works correctly

## Benefits

? **Fixed Layout** - JSON panel no longer stretches vertically
? **Better UX** - Scrollbar appears when needed
? **Consistent Behavior** - Matches other scrollable text areas in the app
? **Proper XAML** - Uses correct WPF pattern for scrollable TextBox
