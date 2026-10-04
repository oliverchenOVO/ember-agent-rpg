# Portfolio and admission copy · 網站／推甄介紹

## Title / 一句話

**EMBER — Autonomous Agent Tower RPG**

以 3D 爬塔遊戲為實驗環境，探索自主決策、有限記憶與群體互動。

A 3D tower RPG as an observable environment for autonomous decisions, finite memory and group interaction.

## 繁體中文短版

EMBER 是以自主 Agent 為核心的 3D 爬塔 RPG。四名角色依人格、資源與關係選擇行動，探索、戰鬥、分隊與留下遺書。我將遊戲作為實驗環境，設計分層決策、來源記憶、可重現模擬與 telemetry，再用配對測試調整策略和數值。目前為可遊玩的研究作品，後段內容與完整平衡驗證仍持續迭代。

## English short version

EMBER is a 3D autonomous-agent tower RPG built with Unity 6 and C#. Four adventurers choose goals and tactics using personality, resources, directed relationships and source-aware memories. The project separates high-level reasoning from local execution, supports independent party progression, and records seeded simulation evidence for testing and balance iteration. It is a playable research and portfolio build, with later-floor content and full balance validation still in progress. Development was AI-assisted and directed by Oliver Chen.

## 繁體中文長版／推甄版

我以 EMBER 作為自主系統的可觀察實驗環境，將角色行為拆解為狀態、目的、候選行動、合法性與執行結果。四名 Agent 在二十五層高塔中自行選擇戰鬥、恢復、探索、製作與撤離；它們可能依關係與不同偏好分隊，也可能因搜尋或討論過久而錯過逃生。

系統採分層架構：高階 Reasoner 以情境和效用評分決定目的，低階處理固定步長的移動、閃避與施法。記憶保留來源與信心，區分親身經歷、遺書和轉述，避免把觀察介面的完整資料直接洩漏給角色。共享導航足跡處理建築障礙，預測治療則估計到達前的傷害與同伴已準備的支援。

工程上，我保留種子、狀態、telemetry、成品雜湊和失敗紀錄，透過受控前後比較調整職業及隊伍策略，也建立有界的 Build／Player 監測，避免把卡住、舊成品或中斷測試誤寫為成功。最新短測有四十三項機制檢查、六種編成比較與雙語介面證據，但我明確保留完整 Balance Gate 和長測尚未重跑的限制。作品展現的是問題拆解、架構、實驗思維與工程迭代，而不只是遊戲完成度。

## English extended version

EMBER uses an autonomous-agent RPG as a testable environment for AI systems engineering. Instead of controlling four characters directly, the player observes goals, tactics, relationships and memories as the Agents attempt a 25-floor tower. The design decomposes behavior into context, bounded proposals, legality checks and local execution, so failures can be explained rather than hidden behind an animation.

High-level decisions use utility scores influenced by personality, resources and directed bonds. Local combat execution handles hazards, skill constraints and team support without waiting for a network service. An optional LLM gateway can propose constrained high-level decisions, with timeouts, validation and local fallback. Memory records distinguish personal experience, inherited Book entries and information from other Agents. Independent party states prevent split groups from sharing Boss health or resetting dangerous rest timers.

The engineering focus is evidence-driven iteration. Seeded fixtures, continuation-based save/load checks, structured telemetry and source/assembly hashes support reproducible comparisons. The latest controlled balance comparison covers six party compositions, two seeds and three floors, alongside 43 mechanism/regression checks and bilingual UI checks. Those results are explicitly scoped: full progression balance and the latest long-running validation remain pending. Build stalls, native allocation warnings, invisible damage and CJK display issues are documented as engineering problems with bounded conclusions. Oliver Chen directed the architecture, design, validation, integration and iteration with AI-assisted development.

## Review talking points

Stack: Unity 6, C#, JSON data tables, custom grid navigation, procedural geometry/audio, Blender-assisted model generation, Python analysis and PowerShell watchdog tools.

Role: design and architecture direction, integration, validation criteria, testing and telemetry-based iteration; AI-assisted development disclosed.

For CS / AI / software-engineering applications, discuss legal-action boundaries, incomplete information, deterministic fixtures, source-aware knowledge and why short clean tests cannot prove global balance or long-run stability. Avoid claiming a trained policy, autonomous general intelligence, or finished commercial game.
