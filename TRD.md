# Technical Requirements Document (TRD)

## Metadata

| Field | Value |
| --- | --- |
| Document | TRD.md |
| Product | Smart Parking Navigator MVP |
| Version | 0.1.0 |
| Status | Draft |
| Based On | PRD.md v0.1.0 |
| Issue | Closes #2 |
| Repository | nychuiminchan-dotcom/Smartparking |
| Last Updated (UTC) | 2026-08-19 |
| Source Inputs | PRD.md, IDEATION.md, data/*, docs/00-setup.md, docs/google-maps-api-key.md, docs/data-gov-sg-api-key.md, src/* scaffold |

## Revision History

| Version | Date (UTC) | Author | Summary |
| --- | --- | --- | --- |
| 0.1.0 | 2026-08-19 | @copilot | Initial MVP technical design aligned to approved PRD. |

## 1. Purpose and Scope

This TRD defines the implementation design for the workshop-sized Smart Parking Navigator MVP specified in PRD.md. It preserves the prepared Aspire scaffold and focuses on destination search in Singapore, nearby HDB car parks, live availability, freshness, filters, and details.

Out of scope: deployment architecture, databases/persistence, MCP integration, and agentic AI in core MVP (deferred to step 05 capstone).

## 2. Architecture Constraints and Component Responsibilities

### 2.1 CarparkAvailability.WebApp

Responsibilities:
- Render mobile-first UI for destination search, map/list, filters, and car park details.
- Integrate Google Maps/Places/Geocoding in browser using frontend key supplied by AppHost.
- Restrict destination selection to Singapore.
- Call ApiApp for all parking data.
- Display data states: loading, empty, stale, unavailable, error.

Must not:
- Call data.gov.sg directly.
- Contain data.gov.sg API key.

### 2.2 CarparkAvailability.ApiApp

Responsibilities:
- Load and validate HDB CSV static data.
- Convert SVY21 coordinates to WGS84 latitude/longitude.
- Poll data.gov.sg car park availability endpoint.
- Parse and validate live payload values safely.
- Match static (`car_park_no`) with live (`carpark_number`) records.
- Maintain immutable last-known-good snapshot for degraded mode.
- Compute 500m geodesic nearby results and apply server-side filters.
- Expose typed HTTP API endpoints for WebApp.
- Return Problem Details for validation and processing errors.

Must not:
- Expose Google Maps key.
- Delegate business filtering correctness to frontend only.

### 2.3 CarparkAvailability.AppHost

Responsibilities:
- Compose WebApp and ApiApp runtime graph.
- Supply `GoogleMaps__ApiKey` to WebApp only.
- Supply `DataGovSg__ApiKey` to ApiApp only.
- Keep secrets in configuration/user-secrets boundary.

### 2.4 CarparkAvailability.ServiceDefaults

Responsibilities:
- Shared service defaults (health endpoints, resilience handlers, telemetry wiring, service discovery).
- No domain logic for parking behavior.

## 3. Data Inputs and Contracts

### 3.1 Static HDB Data (CSV)

Source: `data/HDBCarparkInformation.csv`

Required columns:
- `car_park_no`, `address`, `x_coord`, `y_coord`, `car_park_type`, `type_of_parking_system`, `short_term_parking`, `free_parking`, `night_parking`, `car_park_decks`, `gantry_height`, `car_park_basement`

Parsing/validation requirements:
- Fail fast on missing required headers.
- Validate each row for required fields and parseable numeric coordinates.
- Keep categorical values as raw source values (no undocumented semantic reinterpretation for free/short-term parking).
- Record row-level validation issues with diagnostics.

### 3.2 Live Availability Data (data.gov.sg)

Endpoint: `/v1/transport/carpark-availability` (recommended one-minute polling cadence).

Parsing/validation requirements:
- Parse `carpark_number`, `update_datetime`, and `carpark_info[]`.
- Safely parse numeric string fields (`lots_available`, `total_lots`) without converting invalid text to zero.
- Accept backward-compatible additive fields; detect missing required fields/type mismatches as schema/data errors.
- Interpret timestamps with Singapore Standard Time semantics where required.

## 4. Coordinate Conversion and Distance

### 4.1 SVY21 to WGS84

- Convert HDB `x_coord`/`y_coord` from SVY21 to WGS84 latitude/longitude in ApiApp.
- Validate conversion with known Singapore reference test points and tolerance bounds.
- Rows with unconvertible coordinates are excluded from map/search results and logged as data-quality issues.

### 4.2 Nearby Distance Calculation

- Use geodesic distance on WGS84 coordinates.
- Include only car parks within an exact 500m radius from destination coordinate.
- Return distance in metres for client display and ranking.

## 5. Matching and Aggregation

- Join key: static `car_park_no` ↔ live `carpark_number` (exact canonicalized match, e.g., trimmed and normalized case).
- Preserve static-only records for metadata integrity; availability fields marked unavailable when unmatched.
- Preserve live-only records as unmatched diagnostics; do not fabricate static detail fields.
- Lot-type handling supports C/H/S/Y when present.
- Occupancy is derived only when `total_lots > 0` and parsing is valid.

## 6. Freshness and Last-Known-Good Behavior

Define result status states:
- **Fresh:** live data retrieved and within freshness threshold.
- **Stale:** last-known-good exists but latest fetch exceeded freshness threshold or is delayed.
- **Unavailable:** no usable live snapshot is available.

Behavior requirements:
- Keep immutable last-known-good snapshot in memory.
- On polling/API failure, continue serving last-known-good with stale/unavailable state markers.
- Never present stale/unavailable data as fresh.

## 7. API Boundaries (ApiApp ↔ WebApp)

Minimum API capabilities:
- Destination-based nearby search (Singapore coordinate input, 500m bounded results).
- Filter parameters: available-only, vehicle type, night parking, car park type.
- Car park detail payload including static metadata + availability/freshness summary.
- Data-state indicators in response model (`fresh`, `stale`, `unavailable`, plus update timestamps).

Error contract:
- Validation and domain errors use RFC 7807 Problem Details.
- Upstream failures map to explicit error states without leaking secrets/internal stack details.

## 8. Security and Credential Handling

- `GoogleMaps__ApiKey` is provided to WebApp only; key must be browser-restricted (origin/API restrictions).
- `DataGovSg__ApiKey` is provided to ApiApp only; never sent to browser/client payloads.
- Keys are configured via AppHost user-secrets/environment; never committed.
- Logs and error payloads must not contain secrets.

## 9. Accessibility and UX State Requirements

WebApp must provide:
- Keyboard-accessible search, filters, and result/detail interactions.
- Clear textual and visual announcements for loading/empty/stale/unavailable/error states.
- Readable distance/availability/freshness information on mobile layouts.

## 10. Error Handling and Observability

- Structured logging for CSV validation failures, polling failures, schema/data mismatches, and matching anomalies.
- Health endpoints remain provided by ServiceDefaults.
- Retry/resilience behavior uses existing ServiceDefaults HTTP client defaults.
- Error messages to users are actionable and non-sensitive.

## 11. Testing Strategy

### 11.1 Unit Tests

- CSV header and row validation behaviors.
- SVY21→WGS84 conversion accuracy against known points.
- Numeric parsing safety and lot-type mapping.
- Join/match logic for matched/unmatched records.
- Freshness state transitions and last-known-good behavior.
- Distance filtering at boundary conditions (inside/outside 500m).

### 11.2 API Tests

- Nearby search response contract and filter correctness.
- Details response content and data-state indicators.
- Problem Details for invalid requests and upstream/data failures.

### 11.3 Frontend Tests

- Rendering of loading/empty/stale/unavailable/error states.
- Filter interaction and result synchronization.
- Details panel content rendering.
- Accessibility checks for keyboard/focus and state messaging.

### 11.4 Integration Tests

- End-to-end destination → nearby results → detail view flow using representative prepared data.
- Credential boundary test: WebApp never directly requests data.gov.sg.

## 12. Traceability Matrix (PRD AC → TRD Design)

| PRD Acceptance Criterion | TRD Sections |
| --- | --- |
| AC-1 Destination Search (Singapore) | 2.1, 7, 11.2, 11.4 |
| AC-2 Nearby HDB Car Parks (500m) | 4.2, 5, 7, 11.1, 11.2 |
| AC-3 Live Availability | 3.2, 5, 6, 11.1 |
| AC-4 Freshness & Source Time | 6, 7, 11.1, 11.3 |
| AC-5 Basic Filters | 2.1, 7, 11.2, 11.3 |
| AC-6 Car Park Details | 3.1, 5, 7, 11.2, 11.3 |
| AC-7 Error and Empty States | 7, 9, 10, 11.2, 11.3 |
| AC-8 Scope Guardrails | 1, 8 |

## 13. Deferred Scope Confirmation

Explicitly deferred from this TRD and MVP implementation:
- Deployment/infrastructure design.
- Databases and persistent storage.
- MCP servers/integration.
- Agentic AI inside core app behavior (handled in step 05 capstone).
