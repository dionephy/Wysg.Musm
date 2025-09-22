# Radium: PACS Reporting Spec

## User requests (latest)
- PP1: Focused tree view (levels 1..4 single nodes; expand beyond 5).
- PP2: Operation dropdowns do not open in the grid.

## Updates
- PP1: Implemented focused chain down to level 4 and expanded subtree from that focus level. Fallback to desktop windows if unresolved.
- PP2: Stabilized operation dropdown behavior inside DataGrid cells; do not reset ItemsSource; refresh visuals and maintain dropdown state.

## Overview
- WPF app (.NET 9) with FlaUI automation tools and a dark-themed UI Spy.
- SpyWindow: UI Tree + Crawl Editor + Custom Procedures (dynamic args, var flow).
