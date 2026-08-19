# Product Requirements Document (PRD)

## Metadata

| Field | Value |
| --- | --- |
| Document | PRD.md |
| Product | Smart Parking Navigator MVP |
| Version | 0.1.0 |
| Status | Draft for approval |
| Issue | Closes #2 |
| Repository | nychuiminchan-dotcom/Smartparking |
| Last Updated (UTC) | 2026-08-19 |
| Source Inputs | IDEATION.md, data/*, docs/00-setup.md, docs/google-maps-api-key.md, docs/data-gov-sg-api-key.md, Aspire scaffold in src/ |

## Revision History

| Version | Date (UTC) | Author | Summary |
| --- | --- | --- | --- |
| 0.1.0 | 2026-08-19 | @copilot | Initial workshop-sized MVP PRD draft for approval before TRD. |

## 1. Problem Statement

Drivers in Singapore often reach their destination area without confidence that nearby HDB car parks have available lots or are suitable for their vehicle/parking needs. They need a fast, mobile-first way to find nearby HDB car parks with current availability and clear data freshness.

## 2. Target Users

- **Primary:** Drivers in Singapore searching for parking near a destination.
- **Secondary:** Drivers comparing nearby alternatives when a selected car park is full.

## 3. Product Goals (MVP)

1. Help users find suitable nearby HDB car parks within seconds.
2. Show live lot availability and total lots for supported vehicle lot types.
3. Make freshness explicit so stale/unavailable data is not mistaken as current.
4. Keep scope achievable in a workshop implementation.

## 4. Singapore Context Constraints

- Destination search is restricted to Singapore.
- Nearby car parks are limited to HDB car parks within **500 metres** of the destination.
- Time interpretation for display/logic uses Singapore Standard Time (SGT).
- HDB static data and data.gov.sg live data are authoritative inputs for MVP.

## 5. In-Scope MVP Capabilities

### 5.1 Destination Search & Nearby Discovery

- Search by destination/address in Singapore.
- Return HDB car parks within a 500m radius.
- Show distance from destination to each car park.

### 5.2 Live Lot Availability & Freshness

- Show available lots and total lots by supported lot type (C, H, S, Y when present).
- Show occupancy (derived from available/total lots where valid).
- Show source update time and freshness state per result.

### 5.3 Basic Filters

- Available-only filter.
- Vehicle-type (lot type) filter.
- Night parking filter.
- Car park type filter (e.g., surface, basement, multi-storey).

### 5.4 Car Park Details

At minimum:
- Car park number and address.
- Car park type.
- Parking system type.
- Short-term parking value.
- Free parking value.
- Night parking value.
- Deck count.
- Gantry height.
- Basement indicator.

### 5.5 Required UX States

- Loading
- Empty results
- Stale data
- Unavailable live data (last-known-good context if available)
- Error/failed request

## 6. User Journeys

1. **Find parking near destination**
   - User enters a Singapore destination.
   - App shows nearby HDB car parks within 500m, with distance, availability, occupancy, update time, and freshness.
2. **Filter to suitable options**
   - User applies available-only, vehicle-type, night-parking, and car-park-type filters.
   - Results update to only matching car parks.
3. **Inspect a specific car park**
   - User opens car park details to confirm suitability (height, parking system, conditions).
4. **Handle stale/unavailable data**
   - If live fetch is stale/unavailable, app clearly marks freshness state and avoids presenting stale data as current.

## 7. Explicit Exclusions (Out of Scope)

- Favorites, alerts, accounts, reservations, payments.
- Forecasting/prediction, historical warehousing.
- Traffic/weather integrations.
- New deployment infrastructure and production operations.
- Databases and persistence layers.
- MCP server integration.
- Agentic AI inside core MVP (deferred to **step 05 capstone**).

## 8. Security & Credential Boundaries

- Google Maps credential is frontend-facing and restricted for browser origin/API usage.
- data.gov.sg credential remains backend-only and must not be exposed to the browser.
- No credentials in source control, prompts, logs, or generated docs beyond boundary rules.

## 9. Measurable Acceptance Criteria (P0)

### AC-1 Destination Search (Singapore)
- Given a destination outside Singapore, the app rejects or blocks the query with clear user feedback.
- Given a valid Singapore destination, nearby search executes and returns candidate car parks.

### AC-2 Nearby HDB Car Parks
- Search results include only HDB car parks within 500m geodesic distance from destination.
- Results include distance per car park.

### AC-3 Live Availability
- For each result, app displays available and total lots for supported lot types present in live data.
- Numeric parsing errors do not silently convert unknown values to valid counts.

### AC-4 Freshness & Source Time
- Each result shows source update time and freshness state.
- Stale/unavailable states are visually and textually distinct from fresh data.

### AC-5 Basic Filters
- Available-only, vehicle-type, night-parking, and car-park-type filters are all supported and combinable.
- Filtered results preserve freshness and error state indicators.

### AC-6 Car Park Details
- User can open and view required detail fields listed in Section 5.4.
- Raw categorical values (e.g., free/short-term parking) are shown without undocumented semantic reinterpretation.

### AC-7 Error and Empty States
- Loading, empty, stale, unavailable, and error states are all explicitly rendered.
- Upstream failure does not crash UI; users receive clear state messaging.

### AC-8 Scope Guardrails
- No deployment/database/MCP implementation appears in MVP behavior.
- Agentic AI capabilities are excluded from MVP and deferred to step 05.

## 10. Non-Functional Priorities (MVP Level)

- Mobile-first readability for list/detail states.
- Accessible interaction and state messaging for keyboard and assistive technologies.
- Reasonable response time for a workshop demo flow (destination to results in seconds under normal conditions).

## 11. Traceability to Next Document

TRD.md (after PRD approval) must map each P0 acceptance criterion to concrete component responsibilities and tests across WebApp, ApiApp, AppHost, and ServiceDefaults.
