# Mobile View Rework Design

**Date:** 2026-06-06
**Status:** Approved

## Problem

1. Mobile period selector uses small dropdown chips that cause frequent misclicks
2. Logout button not accessible on mobile (only in avatar dropdown)
3. Company switcher not visible on mobile (only in sidebar footer, which is hidden on mobile)

## Solution

Replace mobile-only UI elements with a right-sliding `UDrawer` component. Desktop/tablet layout remains largely unchanged with improved hit targets.

## Architecture

### Mobile (< 640px)

- `UHeader` right section shows: period button, theme toggle, avatar
- Period button opens a `UDrawer` from the right (standard Nuxt UI width ~280px)
- Drawer contains:
  - Current period display (name + date range)
  - Period selection list (7 options, min-height 44px each)
  - Company switcher (if multiple companies)
  - Logout button
- Left sidebar (`UDashboardSidebar`) is hidden on mobile

### Desktop/Tablet (>= 640px)

- Left sidebar remains visible with period selector in footer
- Period rows in sidebar footer get min-height 44px for better hit rate
- Company switcher added to sidebar footer
- Logout stays in avatar dropdown (unchanged)
- No drawer on desktop

## Component Changes

### `DashboardLayout.vue`

- Add `UDrawer` component for mobile period/settings panel
- Move period button to header right (visible on mobile only)
- Conditionally show drawer trigger based on screen size
- Keep existing desktop header layout

### `SideBar.vue`

- Add company switcher to sidebar footer (desktop only)
- Increase period row min-height to 44px
- Keep existing structure otherwise

### New Component: `MobileSettingsDrawer.vue`

- Extracted drawer component containing:
  - Period selector list
  - Company switcher
  - Logout button
- Uses `UDrawer` from Nuxt UI
- Triggered from header on mobile

## Data Flow

- Period selection uses existing `DataStore.currentPeriod` and `filterItems` from `MenuItems.ts`
- Logout uses existing Auth0 `logout()` function
- Company switcher uses existing `CompanySwitcherModal` component or inline implementation
- All state management unchanged

## Breakpoints

- Mobile: < 640px (drawer active, sidebar hidden)
- Tablet/Desktop: >= 640px (sidebar visible, no drawer)

## Files to Modify

1. `frontend/src/layouts/DashboardLayout.vue` - Add drawer, mobile period button
2. `frontend/src/components/SideBar.vue` - Add company switcher, taller rows
3. `frontend/src/components/MobileSettingsDrawer.vue` - New component
