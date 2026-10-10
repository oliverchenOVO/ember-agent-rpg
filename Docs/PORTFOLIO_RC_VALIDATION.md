# Portfolio Release Candidate 0.3.1 驗證

**PORTFOLIO RC STATUS: BLOCKED** — 正式 Build、119 個介面案例、30 分鐘觀察與解壓驗證通過；實際 Windows IME 組字／候選字流程未取得驗證。公開 Release 不更新，也未建立 v0.3.1 tag／Release。既有 [v0.3.0-portfolio](https://github.com/oliverchenOVO/ember-agent-rpg/releases/tag/v0.3.0-portfolio) 保留原成品。

日期：2026-10-11。基線 `64a29ce`；輸入隔離修正 `e003aa5`、完整 fixture 回復 `fca13ac`；版本資訊／倉庫快照 `ddce826`。本輪沒有改戰鬥、平衡、決策或樓層推進，也沒有跑 5000 seeds／120-minute soak。

## 發布 Gate

| 條件 | 結果 |
| --- | --- |
| 基線與來源 | 基線 clean，本機／遠端 64a29ce 相符；最終文件另提交 |
| Windows Release | PASS，BuildOptions.None，Debug.isDebugBuild=False，0.3.1 |
| 45 既有 UI cases | PASS |
| 新觀察介面 | PASS，14 cases × zh-TW／en |
| Input focus | 數字、Space、左右鍵及 Unicode 中文實機 PASS；IME 組字分支 PASS；實際 OS 候選字 UNVERIFIED → BLOCKER |
| 舊紀錄／存檔 | PASS，current schema 2、schema 1 fixture 遷移、兩種歷史配置格式 |
| zh-TW／English／字型 | PASS；使用內嵌 Noto CJK TC 的 IMGUI，未使用 TextMeshPro；本輪 localization 為 651 keys／899 glyphs |
| 正式 Player 截圖 | PASS，12 類畫面加 1 張 Hero，1920×1080／1600×900；逐張檢視無缺字、截斷、溢出 |
| 30 分鐘觀察 | PASS，完成收據與退出碼 0 |
| 例外／Errors／JobTempAlloc | 各 0，本輪 Player 範圍 |
| 公開安全掃描 | PASS，無確認的憑證；掃描範圍與歷史資料保留限制見安全收據 |
| ZIP／SHA | PASS，CRC、完整解壓、解壓 Player 9 cases、與建置組件相符 |
| 公開 Release 更新 | NO，維持 Gate，未發布候選 ZIP |

## Build 與執行身分

- 新目錄 `Builds/PortfolioRC/Windows/`，未挪用舊 Development Build。最後一次 Build 退出 0，16.42 秒（使用已存在的 shader cache）；首次候選 Build 亦成功。
- Managed assembly SHA-256：`bc6b54e40d4b549cb4a8476c974feed1d5e443041bb04e7138fac1e8e5c3e083`。所有回歸、30 分鐘與解壓測試都比對同一組件。
- 新 UnityPlayer.dll 套用官方 Unity Application Patcher 1.3.3，簽章驗證 Valid／Unity Technologies SF，修補退出 0。
- 修補後 UnityPlayer SHA-256：`d65f4bc3ba5415b1787effe7e9a7c194de0169071ef2f350855278bd8a88fca4`。使用 [Unity 官方修補流程](https://unity.com/security/sept-2025-01/remediation)；未將未修補 runtime 發布。
- 修補工具、PDB、logs、telemetry、temp、Editor 及原始碼均未放入 ZIP。保留必要 Unity／Mono／graphics runtime、README_FIRST、著作權與第三方授權聲明。

## 自動介面回歸

| Player 組別 | 案例 | 尺寸 | 執行時間 | 結果 |
| --- | ---: | --- | ---: | --- |
| gallery-zh | 25 | 1920×1080 | 28.30 秒 | PASS，0 issues |
| hero | 12 | 1920×1080 | 17.19 秒 | PASS，0 issues |
| inspection-zh | 14 | 1280×720 | 18.20 秒 | PASS，0 issues |
| journal-en | 9 | 1280×720 | 14.13 秒 | PASS，0 issues |
| journal-zh | 9 | 1600×900 | 14.23 秒 | PASS，0 issues |
| knowledge-zh | 13 | 1600×900 | 17.18 秒 | PASS，0 issues |
| observer-en | 14 | 1600×900 | 19.27 秒 | PASS，0 issues |
| observer-zh | 14 | 1920×1080 | 19.23 秒 | PASS，0 issues |
| zip | 9 | 1600×900 | 15.24 秒 | PASS，0 issues |

合計 **119 個**，全部使用新正式 Player；45 個原案例保留，沒有以新增案例取代。額外 28 個觀察案例的程式斷言涵蓋：125 筆事件保留最近 100 筆、最新排序、角色／高塔、trim 與大小寫不敏感搜尋、組合搜尋、選取角色、空結果、捲動、語言快取、pause／speed 回復與不變的世界／RNG。

介面案例使用受控 fixture，尤其 100 筆重複事件與八筆歷史紀錄是壓力／相容性測試資料，不是自然生成的挑戰成果。schema 1 fixture 由測試資料序列化，再走實際 Load／Migrate 路徑；不宣稱已覆蓋所有使用者歷史存檔。兩種已知歷史配置顯示實際物品／技能名稱，資料不存在時顯示合理 fallback；不補造過去沒有儲存的裝備品質、技能或數值。

## Windows 實際輸入檢查

使用 Computer Use 的原生 Windows 輸入，正式 Player 搜尋框保持焦點，依序輸入 1、2、3、Space、Left、Right、繁中「巨劍」。畫面時間維持 00:24、速度維持 1 倍，鏡頭未跟著方向鍵轉動；已確認文字游標與搜尋內容。

OS IME 候選窗未能出現，故**不能把 Unicode 注入當成 IME 組字／選字通過**。程式回歸已檢查 composing=true 期間 digit／Space／arrow 不改控制，Escape 不關閉閱讀，但仍缺實際 Windows 輸入法的候選字、數字選字及組字 Escape 流程。測試視窗在 180 秒短測結束後正常退出；最後一次 Escape 操作因視窗已關閉而未能完成，沒有把該次操作寫成實機通過。關閉回復以自動案例及下列 30 次長測操作驗證。

## 30 分鐘正常觀察

Windows 11 64-bit（10.0.26200）、RTX 3070 Ti Laptop、Direct3D12。單一正式 Player，固定起始種子 1729，1 倍速自然推進；未同時跑 headless cohort 或另一個 Player。診斷腳本只操作閱讀、篩選、捲動、選角、關係頁、語言與存讀檔，未補血、跳層或重設 Boss。

- Profile 取樣 1801.004 秒、283,894 幀；process wall 1807.41 秒，退出 0，正常 scene cleanup 完成。
- 30 次閱讀開啟／關閉，每次保持 8 秒；逐幀比對世界／RNG，關閉後比對原 paused／speed。10 次 Save／Load，7 次語言切換，10 次選擇關係頁。
- 同一輪自然觀察到 1–15 樓；沒有實際輪迴重啟、沒有二十五層通關，也沒有用該樣本宣稱勝率。事件中的 restart=1 是起始 run 標記。
- History 在這一輪尚未完成前是空紀錄，長測覆蓋空狀態；非空資料與舊配置由有界 UI fixture 覆蓋。
- Exceptions 0、error lines 0、JobTempAlloc 0；未記錄 UI state corruption。
- 所有測試 save／checkpoint 使用專用 artifact 路徑，未覆寫玩家的正常存檔。

| 指標 | 暖機後 300–480 秒 | 最後 1620–1800 秒 |
| --- | ---: | ---: |
| 平均 FPS（含閱讀） | 156.33 | 157.00 |
| 平均 FPS（排除閱讀診斷） | 157.76 | 159.86 |
| 中位幀時間 | 6.065 ms | 6.075 ms |
| P95 幀時間 | 7.763 ms | 8.321 ms |
| Unity total used 中位數 | 226.45 MiB | 239.51 MiB |
| GC used 中位數 | 4.50 MiB | 5.27 MiB |

平均 FPS 變化 +0.43%，未見平均 FPS 退化；P95 有小幅增加。Unity 使用中記憶體中位數增加 13.06 MiB（不同場景與累積資料）；process working set 峰值 807.40 MiB、末次 784.95 MiB，後段維持約 770–795 MiB。這是一個正常工作負載樣本，不能證明沒有任何洩漏、所有硬體皆穩定或歷史 native warnings 永久修復。前段另有文件／檔案安全掃描，未同時進行大型遊戲測試。

## ZIP 與來源

`EMBER-Windows-x64.zip`：47,346,533 bytes，141 files，CRC PASS。完整解壓後執行新 Player，9 個紀事案例退出 0／layout issues 0，assembly hash 相符。

SHA-256：`c52c63f561b246f6e4c6e4689961083b1ee687848f73e2654a199d96568a3523`。

[機器可讀驗證](../Evidence/portfolio-rc-validation.json) · [安全收據](../Evidence/portfolio-rc-safety.json) · [截圖與來源](PORTFOLIO_RC_SCREENSHOTS.md) · [倉庫設定快照](PORTFOLIO_RC_REPOSITORY_SNAPSHOT.md)。原始 Editor／Player／frames／events／process logs 均留在忽略的 `Artifacts/Phase311/portfolio-rc-*`，不公開本機路徑或私人報告。

最終公開內容快照共 523 files，新增文件、13 張圖片、分析器與收據已重新掃描；95 個 localization／tower 識別字命中與已審閱快照完全相符，沒有新增確認憑證。完整歷史掃描的來源為 `ddce826`（70 commits）；其後僅提交本輪文件、素材與分析工具，原始私人掃描報告保持忽略。

## 收尾與下一步

README／網站／推甄介紹已補上 observer-safe simulation inspection；Hero 是正式 Release 的受控實機畫面，沒有生成或修改遊戲內容。社群預覽素材與候選首圖具備來源紀錄，GitHub preview 設定仍未更新。完成來源／驗證文件提交後保持工作目錄 clean。

**目前停止發布，等待下一步。** 必須先取得實際 IME 組字／候選字驗證，再重新確認所需 Gate；本輪不再啟動長測。保留歷史 Balance Gate FAIL，最新 5000-seed／120-minute soak 未執行。後段內容、動畫、音效與平衡仍為研究原型；不是正式商業品質認證。
