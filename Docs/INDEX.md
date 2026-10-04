# Documentation index

從[繁體中文 README](../README.md)開始，或閱讀 [English README](../README.en.md)。

## Portfolio and current implementation

- [Real Player gallery](SCREENSHOTS.md): versions and controlled fixtures are labeled.
- [Architecture](ARCHITECTURE.md), [Agent decisions](AGENT_DECISION_LOGIC.md), [algorithms](ALGORITHMS.md), [memory and relationships](MEMORY_RELATIONSHIPS.md).
- [Engineering evidence](ENGINEERING_EVIDENCE.md), [case study](PORTFOLIO_CASE_STUDY.md), [website/admission copy](PORTFOLIO_COPY.md).
- [Known limitations](../KNOWN_LIMITATIONS.md), [distribution status](DISTRIBUTION.md), [publication audit](PUBLIC_REPOSITORY_SAFETY_AUDIT.md), [repository settings](PUBLIC_REPOSITORY_SETTINGS.md).

## Latest gameplay validation

[Team Balance](TEAM_BALANCE_VALIDATION.md) belongs to gameplay commit `43a2f70`. Subsequent portfolio commits do not introduce new gameplay or imply a new full balance certification. The latest 5000-seed run and 120-minute soak were not run.

## Historical development evidence

Earlier design/validation documents remain in Git for traceability. Their build paths, test totals and PASS/FAIL statements refer to their own dated revisions. Local paths are historical records, not downloadable GitHub links.

- [Phase 2](PHASE2_VALIDATION.md), [Phase 3](PHASE3_VALIDATION.md), [Phase 3.1](PHASE3_1_VALIDATION.md), [Phase 3.1.1](PHASE3_1_1_VALIDATION.md). Phase 3.1.1 full Gate failed.
- [Rest interaction](REST_INTERACTION_VALIDATION.md), [knowledge codex](KNOWLEDGE_CODEX_VALIDATION.md), [combat art](COMBAT_ART_VALIDATION.md), [refuge exploration](REFUGE_EXPLORATION_VALIDATION.md).

Unity source lives under `UnityProject`; scripts for builds, bounded Player checks and historical simulations are in `Tools`. **Inspect a script before running it: `build.ps1 -TestsOnly` includes a 5000-seed batch.**
