# 31 — Frontend Vite Architecture

## Stack
React, TypeScript, Vite, Tailwind CSS, React Router, TanStack Query, Zustand for limited UI state, ECharts/Recharts and Lucide.
Vite officially supports React + TypeScript templates. citeturn0search6

## Areas
Dashboard, Matches, Competitions, Teams, Bookmakers, Markets, Odds Explorer, Match Analysis, Historical Patterns, Similar Matches, Backtesting, Data Quality and Admin.

## Match Detail
Overview, Odds, Odds Movement, Bookmakers, Markets, Events, Statistics, Form, Standings, H2H, Similar Matches, Historical Patterns, Models and Data Quality.

## State
TanStack Query owns server state. Zustand is only for local UI state such as filters and panel state.

## Charts
Odds movement timeline, bookmaker matrix, market distributions, result distributions, team form, statistics and historical patterns.

## Performance
Server-side filtering, pagination/range queries, lazy tabs and virtualization for large lists. Never load every historical odds snapshot into the browser at once.

## Security
No provider API keys in frontend code.
