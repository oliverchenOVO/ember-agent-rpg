# Portfolio RC 倉庫設定快照

記錄日期：2026-10-11。此輪未修改任何安全設定，未邀請協作者。

| 項目 | 發布前狀態 |
| --- | --- |
| 倉庫 | https://github.com/oliverchenOVO/ember-agent-rpg |
| Visibility | Public |
| Origin | HTTPS，與上列倉庫相同 |
| Default branch | main |
| 本機與遠端基線 | 64a29ceea98d8d4b3d3174a0b9817ade5ba718aa；工作目錄 clean |
| 最新公開版本 | v0.3.0-portfolio，prerelease，2026-10-04 |
| Release tag commit | 371eeb5decb454b7a5398a1f13b36d4db188cfb6 |
| Gameplay source of prior package | 43a2f70 |
| main 保護 | enforce_admins；禁止 force push / deletion；要求 resolve conversations |
| Required checks / reviews | 未設定 |
| Rulesets | 無 |
| Collaborators | 僅 oliverchenOVO，admin |
| Issues / Discussions / Wiki / auto merge | 全部停用 |
| Stars / forks | 0 / 0 |

透過 GitHub REST API 與本機 Git 讀取。`releases/latest` 不含 prerelease，回傳 404；改用 releases 清單確認最新公開 prerelease，並以 tag 解出實際提交，未將 target_commitish 的 `main` 當作固定版本。
