# Unity Analytics setup

The project now declares Analytics SDK **6.3.0** and Services Core **1.16.0**, compatible with its Unity 6000.6 editor. The modern UGS SDK is used; the old built-in Analytics switch is not enabled. Local JSON/CSV analytics remain available independently.

## Current state

SDK dependencies, consent controls and gameplay hooks are implemented. On October 19, 2026, all three custom events were created and enabled in the Prismatch production environment through Unity Dashboard. Their custom parameter names and types were verified against `unity_analytics_events.json`: 13 for `pm_power_picked`, 30 for `pm_round_finished`, and 33 for `pm_power_round`, using 35 shared parameter definitions. `Assets/Resources/UnityAnalyticsConfig.json` now has `schemaReady: true`. Player consent remains required, and actual gameplay delivery has not yet been verified.

## Activation and verification

1. Open the project in Unity and allow Package Manager to resolve the new Analytics dependency.
2. Sign in at [Unity Dashboard](https://cloud.unity.com/), select the existing **Prismatch** project in organization **dphart9**, and enable Analytics. The linked project ID is `c1719047-16f0-4105-b63b-f1f56bb720e4`. Unity says only organization owners can sign up for Analytics.
3. Select the intended environment. The configuration currently uses **production**; if you create a separate playtest environment, put its exact name in the configuration instead.
4. In Analytics **Event Manager**, create and enable `pm_power_picked`, `pm_round_finished` and `pm_power_round`. Assign the exact names/types from [unity_analytics_events.json](unity_analytics_events.json), reusing shared parameter definitions between events. The JSON is a generated schema reference, not a dashboard bulk-import/API payload. Names are case-sensitive; all custom parameter names use the `pm_` prefix to avoid reserved names.
5. Change `schemaReady` to `true` after all three schemas are enabled, then restart/rebuild the game.
6. Open **Settings â†’ Analytics / Power Testing â†’ Cloud: Allow Uploads**. The UI explains that gameplay metrics and SDK device/session identifiers go to Unity. Until the player opts in, collection stays off. Turning it off uses Unity's consent framework; the Unity privacy policy and a cloud-data deletion control are available in the same panel.
7. Start a new run, choose powers and complete or fail a round. Verify valid events in Unity's **Event Browser**. The SDK normally uploads batches every 60 seconds; round completion also requests a flush. Uploads and dashboard acceptance have not been verified in this workspace.

## Events

| Event | Frequency | Purpose |
|---|---|---|
| pm_power_picked | Every selected power after opt-in | Run, round, attempt, exact pick order, power name/tags and current build |
| pm_round_finished | Each finished, failed or gracefully abandoned round attempt | Plays, successes, points, credit, duration, cascades, shuffles, objective, target, rules, bosses, budgets and active build |
| pm_power_round | Once per distinct owned power at the end of a won/failed round | Same round statistics plus power name, owned copy count and first active pick order |

Individual swap events and detailed match breakdowns stay in the local log. Cloud events use the existing random run ID to join choices to round attempts; the SDK supplies installation/session identifiers. No player email or account name is added by custom game code. Previously saved local files are never replayed to Unity. If collection is enabled mid-run, subsequent events can describe the currently active build, but earlier pick events are not backfilled.

SDK initialization and dispatch errors do not stop gameplay. Events created after consent while initialization is in progress have a bounded in-memory queue (128 events). On initialization failure they are dropped from that queue and local history remains available. After successful initialization the Unity SDK manages batching and offline delivery. Disabling consent clears this adapter's pending queue. Opt-out does not delete already uploaded data; **Delete Cloud Data** denies consent and requests deletion through the SDK, while retaining local CSV history.

## Comparing power strength

Use `pm_power_round` and group by `pm_power_name` and `pm_copies`. Filter to matching level, round, game revision, goal, mode, board, gravity, objective, target, matching rules, bosses and budget. Consider first pick order and full build when evaluating interactions.

Compute weighted points/play as **sum(pm_points) / sum(pm_plays)**, and collection credit/play as **sum(pm_goal_credit) / sum(pm_plays)**, handling a zero denominator. Averaging each round's precomputed average gives unequal-length rounds the wrong weight. Count won vs failed round outcomes for win rate. Abandoned rounds have no power-round samples.

These metrics describe performance while a power was owned, not its isolated causal contribution. Multiple powers can multiply one another. Increment the game's player version when changing balance so results from different revisions can be separated.

## Pricing checked

Unity currently provides a free tier of **50,000 monthly active users across all projects/environments in the organization**. Above that, usage is paid; the first paid tier is $0.00360 per additional MAU. Fair-use guidance applies after surpassing the free tier and includes 500 custom events per MAU. These are not 500 free events per account. Personal playtesting is well below the MAU free threshold, but organization-wide usage should still be checked in the dashboard.

Official references: [pricing](https://docs.unity.com/en-us/analytics/pricing-and-billing/pricing), [MAU tiers](https://docs.unity.com/en-us/analytics/pricing-and-billing/mau-based-pricing), [fair-use limits](https://docs.unity.com/en-us/analytics/pricing-and-billing/fair-usage-limits), [SDK setup and consent](https://docs.unity.com/en-us/analytics/get-started/get-started), [custom event creation](https://docs.unity.com/en-us/analytics/events/custom-event), and [recording events](https://docs.unity.com/en-us/analytics/events/record-event).

## Validation

The game compiles with the real Analytics/Core SDK source assemblies and the cloud integration enabled. Thirty-eight focused standalone tests pass, including four Unity event tests for exact retry/pick order, raw totals and block credit, distinct-power copy counts, exclusions for abandoned attempts, valid schema names and supported primitive parameter types. The schema reference is generated from the same event constructors used for dispatch.

The SDK package was downloaded from Unity's official registry for compile verification; the editor's normal package import, consent UI, real network delivery, gameplay delivery and dashboard queries still need an end-to-end Unity/account pass. The remote schemas are now created, enabled and verified. No actual gameplay telemetry was sent while performing these checks.

## SDK detection fix

The Analytics SDK is a required package and assembly reference. The adapter now compiles against it directly, without an optional version-define fallback that could silently report installation pending. After Unity imports these changes and completes compilation, restart Play Mode or rebuild the installed game. Existing builds retain their previous integration until rebuilt. Consent remains opt-in.
